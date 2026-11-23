using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NoComments.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.SingleLineComment, Rules.MultiLineComment, Rules.DocumentationComment, Rules.RazorComment);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
        context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);
    }

    private static void AnalyzeAdditionalFile(AdditionalFileAnalysisContext context)
    {
        var path = context.AdditionalFile.Path;
        if (!IsRazorPath(path))
        {
            return;
        }

        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null)
        {
            return;
        }

        foreach (var span in RazorCommentScanner.FindComments(text.ToString()))
        {
            var location = Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
            context.ReportDiagnostic(Diagnostic.Create(Rules.RazorComment, location));
        }
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);

        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            var rule = RuleFor(trivia);
            if (rule is null)
            {
                continue;
            }

            var location = CommentLocation(context.Tree, trivia);
            if (!context.IsGeneratedCode || IsWrittenInRazorFile(location))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, location));
            }
        }
    }

    private static bool IsWrittenInRazorFile(Location location)
    {
        var mapped = location.GetMappedLineSpan();
        return mapped.HasMappedPath && IsRazorPath(mapped.Path);
    }

    private static bool IsRazorPath(string path)
    {
        return path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
    }

    private static DiagnosticDescriptor? RuleFor(SyntaxTrivia trivia) =>
        trivia.Kind() switch
        {
            SyntaxKind.SingleLineCommentTrivia => Rules.SingleLineComment,
            SyntaxKind.MultiLineCommentTrivia => Rules.MultiLineComment,
            SyntaxKind.SingleLineDocumentationCommentTrivia => Rules.DocumentationComment,
            SyntaxKind.MultiLineDocumentationCommentTrivia => Rules.DocumentationComment,
            _ => null,
        };

    private static Location CommentLocation(SyntaxTree tree, SyntaxTrivia trivia)
    {
        if (!trivia.HasStructure)
        {
            return trivia.GetLocation();
        }

        var span = trivia.FullSpan;
        var text = tree.GetText();
        var end = span.End;
        while (end > span.Start && text[end - 1] is '\r' or '\n')
        {
            end--;
        }

        return Location.Create(tree, TextSpan.FromBounds(span.Start, end));
    }
}
