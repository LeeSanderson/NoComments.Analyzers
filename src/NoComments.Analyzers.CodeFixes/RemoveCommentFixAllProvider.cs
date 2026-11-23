using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace NoComments.Analyzers.CodeFixes;

internal sealed class RemoveCommentFixAllProvider : FixAllProvider
{
    internal static readonly RemoveCommentFixAllProvider Instance = new();

    private RemoveCommentFixAllProvider()
    {
    }

    public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() =>
        [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        var diagnostics = await DiagnosticsInScopeAsync(fixAllContext).ConfigureAwait(false);
        if (diagnostics.IsEmpty)
        {
            return null;
        }

        var solution = fixAllContext.Solution;
        return CodeAction.Create(
            RemoveCommentCodeFixProvider.Title,
            cancellationToken => RemoveAsync(solution, diagnostics, cancellationToken));
    }

    internal static async Task<Solution> RemoveAsync(
        Solution solution,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var byRoute = diagnostics.ToLookup(IsMappedToRazorFile);

        var razorComments = byRoute[true]
            .Select(diagnostic => diagnostic.Location.GetMappedLineSpan())
            .ToLookup(mapped => mapped.Path, mapped => mapped.Span, StringComparer.OrdinalIgnoreCase);

        var commentsByDocument = byRoute[false]
            .Select(diagnostic => (Document: solution.GetDocument(diagnostic.Location.SourceTree), Comment: diagnostic.Location.SourceSpan))
            .Where(target => target.Document is not null)
            .GroupBy(target => target.Document!.Id, target => target.Comment);

        foreach (var comments in commentsByDocument)
        {
            var document = solution.GetDocument(comments.Key)!;
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            solution = solution.WithDocumentText(comments.Key, CommentRemoval.Remove(text, comments.Distinct().ToImmutableArray()));
        }

        foreach (var comments in razorComments)
        {
            foreach (var documentId in RazorDocumentIds(solution, comments.Key))
            {
                var text = await solution.GetAdditionalDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var spans = comments.Select(text.Lines.GetTextSpan).Distinct().ToImmutableArray();
                solution = solution.WithAdditionalDocumentText(documentId, CommentRemoval.Remove(text, spans));
            }
        }

        return solution;
    }

    private static bool IsMappedToRazorFile(Diagnostic diagnostic)
    {
        var mapped = diagnostic.Location.GetMappedLineSpan();
        return mapped.HasMappedPath && CommentAnalyzer.IsRazorPath(mapped.Path);
    }

    private static IEnumerable<DocumentId> RazorDocumentIds(Solution solution, string path) =>
        solution.Projects
            .SelectMany(project => project.AdditionalDocuments)
            .Where(document => document.FilePath is { } filePath && SamePath(filePath, path))
            .Select(document => document.Id)
            .ToList();

    private static bool SamePath(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static async Task<ImmutableArray<Diagnostic>> DiagnosticsInScopeAsync(FixAllContext fixAllContext)
    {
        switch (fixAllContext.Scope)
        {
            case FixAllScope.Document when fixAllContext.Document is { } document:
                return await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            case FixAllScope.Project:
                return await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false);
            case FixAllScope.Solution:
                var all = ImmutableArray.CreateBuilder<Diagnostic>();
                foreach (var project in fixAllContext.Solution.Projects)
                {
                    all.AddRange(await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false));
                }

                return all.ToImmutable();
            default:
                return ImmutableArray<Diagnostic>.Empty;
        }
    }
}
