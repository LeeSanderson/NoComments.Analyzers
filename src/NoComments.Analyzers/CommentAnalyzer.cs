using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoComments.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.SingleLineComment, Rules.MultiLineComment);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);

        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            var rule = RuleFor(trivia);
            if (rule is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, trivia.GetLocation()));
            }
        }
    }

    private static DiagnosticDescriptor? RuleFor(SyntaxTrivia trivia) =>
        trivia.Kind() switch
        {
            SyntaxKind.SingleLineCommentTrivia => Rules.SingleLineComment,
            SyntaxKind.MultiLineCommentTrivia => Rules.MultiLineComment,
            _ => null,
        };
}