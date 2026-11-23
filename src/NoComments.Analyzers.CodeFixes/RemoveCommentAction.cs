using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace NoComments.Analyzers.CodeFixes;

internal static class RemoveCommentAction
{
    internal const string Title = "Remove comment";

    internal static CodeAction Create(Solution solution, ImmutableArray<Diagnostic> diagnostics) =>
        CodeAction.Create(
            Title,
            cancellationToken => RemoveAsync(solution, diagnostics, cancellationToken),
            equivalenceKey: Title);

    private static async Task<Solution> RemoveAsync(
        Solution solution,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var byRoute = diagnostics.ToLookup(diagnostic => RazorFile.IsSourceOf(diagnostic.Location));
        var inRazorFiles = byRoute[true];
        var inDocuments = byRoute[false];

        solution = await RemoveFromDocumentsAsync(solution, inDocuments, cancellationToken).ConfigureAwait(false);
        return await RemoveFromRazorFilesAsync(solution, inRazorFiles, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Solution> RemoveFromDocumentsAsync(
        Solution solution,
        IEnumerable<Diagnostic> diagnostics,
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

    private static async Task<Solution> RemoveFromRazorFilesAsync(
        Solution solution,
        IEnumerable<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var commentsByPath = diagnostics
            .Select(diagnostic => diagnostic.Location.GetMappedLineSpan())
            .ToLookup(mapped => mapped.Path, mapped => mapped.Span, StringComparer.OrdinalIgnoreCase);

        foreach (var comments in commentsByPath)
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

    private static List<DocumentId> RazorDocumentIds(Solution solution, string path) =>
        solution.Projects
            .SelectMany(project => project.AdditionalDocuments)
            .Where(document => document.FilePath is { } filePath && SamePath(filePath, path))
            .Select(document => document.Id)
            .ToList();

    private static bool SamePath(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) => path.Replace('\\', '/');
}
