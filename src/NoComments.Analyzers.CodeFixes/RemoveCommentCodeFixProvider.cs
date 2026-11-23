using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace NoComments.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveCommentCodeFixProvider))]
[Shared]
public sealed class RemoveCommentCodeFixProvider : CodeFixProvider
{
    internal const string Title = "Remove comment";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.SingleLineComment.Id, Rules.MultiLineComment.Id, Rules.DocumentationComment.Id);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var comment = diagnostic.Location.SourceSpan;
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    cancellationToken => RemoveAsync(context.Document, comment, cancellationToken),
                    equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> RemoveAsync(Document document, TextSpan comment, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return document.WithText(CommentRemoval.Remove(text, ImmutableArray.Create(comment)));
    }
}
