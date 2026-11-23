using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace NoComments.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveCommentCodeFixProvider))]
[Shared]
public sealed class RemoveCommentCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.SingleLineComment.Id, Rules.MultiLineComment.Id, Rules.DocumentationComment.Id);

    public override FixAllProvider GetFixAllProvider() => RemoveCommentFixAllProvider.Instance;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var solution = context.Document.Project.Solution;
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(RemoveCommentAction.Create(solution, [diagnostic]), diagnostic);
        }

        return Task.CompletedTask;
    }
}
