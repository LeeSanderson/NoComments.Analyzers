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
        var commentsByDocument = diagnostics
            .Select(diagnostic => (Document: solution.GetDocument(diagnostic.Location.SourceTree), Comment: diagnostic.Location.SourceSpan))
            .Where(target => target.Document is not null)
            .GroupBy(target => target.Document!.Id, target => target.Comment);

        foreach (var comments in commentsByDocument)
        {
            var document = solution.GetDocument(comments.Key)!;
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            solution = solution.WithDocumentText(comments.Key, CommentRemoval.Remove(text, comments.Distinct().ToImmutableArray()));
        }

        return solution;
    }

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
