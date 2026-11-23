using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoComments.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.SingleLineComment);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var comments = root
            .DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia));

        foreach (var comment in comments)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.SingleLineComment, comment.GetLocation()));
        }
    }
}
