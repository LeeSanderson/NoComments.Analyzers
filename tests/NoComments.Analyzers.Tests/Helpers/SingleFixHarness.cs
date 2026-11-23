using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NoComments.Analyzers.CodeFixes;
using Shouldly;

namespace NoComments.Analyzers.Tests.Helpers;

internal static class SingleFixHarness
{
    private const int MaximumFixes = 100;

    internal static async Task<string> ApplySingleFixesUntilNoneRemainAsync(string source, DocumentationMode documentationMode)
    {
        using var workspace = new AdhocWorkspace();
        var document = AddDocument(workspace, source, documentationMode);
        var provider = new RemoveCommentCodeFixProvider();

        for (var fixesApplied = 0; fixesApplied < MaximumFixes; fixesApplied++)
        {
            var diagnostics = await AnalyzeAsync(document);
            if (diagnostics.IsEmpty)
            {
                return (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
            }

            var diagnostic = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).First();
            provider.FixableDiagnosticIds.ShouldContain(diagnostic.Id);
            var action = await SingleCodeActionForAsync(provider, document, diagnostic);
            document = await ApplyAsync(action, document.Id);
        }

        throw new InvalidOperationException($"Comments were still reported after {MaximumFixes} fixes.");
    }

    private static Document AddDocument(AdhocWorkspace workspace, string source, DocumentationMode documentationMode)
    {
        var projectId = ProjectId.CreateNewId();
        var project = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            name: "Sample",
            assemblyName: "Sample",
            language: LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview, documentationMode));
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(project)
            .AddDocument(documentId, "Sample.cs", SourceText.From(source));
        return solution.GetDocument(documentId).ShouldNotBeNull();
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Document document)
    {
        var compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        return await compilation
            .WithAnalyzers([new CommentAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<CodeAction> SingleCodeActionForAsync(CodeFixProvider provider, Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), TestContext.Current.CancellationToken);
        await provider.RegisterCodeFixesAsync(context);
        var single = actions.ShouldHaveSingleItem();
        single.Title.ShouldBe("Remove comment");
        return single;
    }

    private static async Task<Document> ApplyAsync(CodeAction action, DocumentId documentId)
    {
        var operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        var changedSolution = operations.OfType<ApplyChangesOperation>().ShouldHaveSingleItem().ChangedSolution;
        return changedSolution.GetDocument(documentId).ShouldNotBeNull();
    }
}
