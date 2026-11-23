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

    internal static async Task<string> ApplyFixAllAsync(
        string source,
        DocumentationMode documentationMode,
        FixAllScope scope,
        string? equivalenceKey)
    {
        using var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var first = AddProject(ref solution, "First", documentationMode, source, source);
        var second = AddProject(ref solution, "Second", documentationMode, source);
        var trigger = first[0];

        var provider = new RemoveCommentCodeFixProvider();
        var fixAllProvider = provider.GetFixAllProvider().ShouldNotBeNull();
        fixAllProvider.GetSupportedFixAllScopes().ShouldContain(scope);
        var context = new FixAllContext(
            solution.GetDocument(trigger).ShouldNotBeNull(),
            provider,
            scope,
            equivalenceKey,
            provider.FixableDiagnosticIds,
            new AnalyzerDiagnosticProvider(),
            TestContext.Current.CancellationToken);

        var action = (await fixAllProvider.GetFixAsync(context)).ShouldNotBeNull();
        var operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        var fixedSolution = operations.OfType<ApplyChangesOperation>().ShouldHaveSingleItem().ChangedSolution;

        var inScope = scope switch
        {
            FixAllScope.Document => [trigger],
            FixAllScope.Project => first,
            _ => [.. first, .. second],
        };
        var result = await TextOfAsync(fixedSolution, trigger);
        foreach (var documentId in first.Concat(second))
        {
            (await TextOfAsync(fixedSolution, documentId)).ShouldBe(inScope.Contains(documentId) ? result : source);
        }

        return result;
    }

    private static async Task<string> TextOfAsync(Solution solution, DocumentId documentId)
    {
        var document = solution.GetDocument(documentId).ShouldNotBeNull();
        return (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
    }

    private static Document AddDocument(AdhocWorkspace workspace, string source, DocumentationMode documentationMode)
    {
        var solution = workspace.CurrentSolution;
        var documentId = AddProject(ref solution, "Sample", documentationMode, source)[0];
        return solution.GetDocument(documentId).ShouldNotBeNull();
    }

    private static ImmutableArray<DocumentId> AddProject(
        ref Solution solution,
        string name,
        DocumentationMode documentationMode,
        params string[] sources)
    {
        var projectId = ProjectId.CreateNewId();
        var project = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            name: name,
            assemblyName: name,
            language: LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview, documentationMode));
        solution = solution.AddProject(project);

        var documentIds = ImmutableArray.CreateBuilder<DocumentId>();
        for (var i = 0; i < sources.Length; i++)
        {
            var documentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddDocument(documentId, $"{name}{i}.cs", SourceText.From(sources[i]));
            documentIds.Add(documentId);
        }

        return documentIds.ToImmutable();
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Document document) =>
        AnalyzeAsync(document.Project);

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Project project)
    {
        var compilation = (await project.GetCompilationAsync(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        return await compilation
            .WithAnalyzers([new CommentAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    private sealed class AnalyzerDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return (await AnalyzeAsync(document.Project)).Where(d => d.Location.SourceTree == tree);
        }

        public override async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            (await AnalyzeAsync(project)).Where(d => !d.Location.IsInSource);

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            await AnalyzeAsync(project);
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
