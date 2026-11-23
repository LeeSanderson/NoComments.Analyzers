using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace NoComments.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveCommentCodeFixProvider))]
[Shared]
public sealed class RemoveCommentCodeFixProvider : CodeFixProvider
{
    internal const string Title = "Remove comment";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.SingleLineComment.Id, Rules.MultiLineComment.Id, Rules.DocumentationComment.Id);

    public override FixAllProvider GetFixAllProvider() => RemoveCommentFixAllProvider.Instance;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var solution = context.Document.Project.Solution;
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    cancellationToken => RemoveCommentFixAllProvider.RemoveAsync(solution, [diagnostic], cancellationToken),
                    equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
