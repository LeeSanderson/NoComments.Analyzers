using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RazorFileShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task ReportCommentsInACodeBlockAtTheirPositionInTheRazorFile()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Counter.razor",
            """
            <p>@count</p>

            @code {
                // single
                private int count = 1; /* multi */
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Counter.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 4, 5, 4, 14),
            new ReportedDiagnostic("NOC0002", 5, 28, 5, 39),
        ]);
    }

    [Fact]
    public async Task ReportCommentsInStatementAndExpressionBlocksOfAComponent()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Greeting.razor",
            """
            @{
                // in a code block
                var name = "World";
            }
            <p>@(name /* in an expression */)</p>

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Greeting.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 2, 5, 2, 23),
            new ReportedDiagnostic("NOC0002", 5, 11, 5, 33),
        ]);
    }

    [Fact]
    public async Task ReportCommentsInEveryCodeConstructOfAPage()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Pages/Index.cshtml",
            """
            @page
            @{
                // in a code block
                var total = Twice(1);
            }
            <p>@(total /* in an expression */)</p>

            @functions {
                // in functions
                private static int Twice(int value) => value * 2;
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Pages/Index.cshtml").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 3, 5, 3, 23),
            new ReportedDiagnostic("NOC0002", 6, 12, 6, 34),
            new ReportedDiagnostic("NOC0001", 9, 5, 9, 20),
        ]);
    }

    [Fact]
    public async Task ReportDocumentationCommentsInACodeBlockAsNoc0003()
    {
        using var project = await CreateRazorProjectBuilderAsync([("GenerateDocumentationFile", "true")]);
        await project.AddFileAsync(
            "Components/Counter.razor",
            """
            <p>@Count</p>

            @code {
                /// <summary>The count.</summary>
                public int Count { get; set; }
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Counter.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0003", 4, 5, 4, 38),
        ]);
    }

    [Fact]
    public async Task ReportOnlyHandWrittenCommentsAmongDirectivesAndRenderFragments()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/MainLayout.razor",
            """
            @namespace test.Layouts
            @inherits LayoutComponentBase
            <main>@Body</main>

            """);
        await project.AddFileAsync(
            "Components/Grid.razor",
            """
            @typeparam TItem
            @using Microsoft.AspNetCore.Components.Web
            @inject Microsoft.Extensions.Configuration.IConfiguration Configuration
            @implements IDisposable
            @attribute [System.Diagnostics.DebuggerDisplay("grid")]
            <ul>
                @foreach (var item in Items)
                {
                    <li @onclick="() => Selected = item">@Row(item)</li>
                }
            </ul>
            @code {
                [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];
                [Parameter] public RenderFragment<TItem> Row { get; set; } = default!;
                private TItem? Selected { get; set; }
                public void Dispose() => Selected = default; // grid
            }

            """);
        await project.AddFileAsync(
            "Components/Home.razor",
            """
            @page "/home"
            @layout test.Layouts.MainLayout
            @preservewhitespace true
            <Grid TItem="string" Items="names">
                <Row>@* row *@<span>@context</span></Row>
            </Grid>
            @{ RenderFragment footer = @<footer>@names.Count</footer>; }
            @footer
            @code {
                private readonly List<string> names = ["a", "b"]; // names
            }

            """);
        await project.AddFileAsync(
            "Pages/Index.cshtml",
            """
            @page "{id:int?}"
            @model test.Pages.IndexModel
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
            @inject Microsoft.Extensions.Configuration.IConfiguration Configuration
            <form method="post"><input asp-for="Name" /></form>
            @section Scripts { <script></script> }
            @{ /* in page */ }

            """);
        await project.AddFileAsync(
            "Pages/Index.cshtml.cs",
            """
            namespace test.Pages;

            public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
            {
                public string Name { get; set; } = "";
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Grid.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 16, 50, 16, 57),
        ]);
        buildOutput.DiagnosticsIn("Components/Home.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0004", 5, 10, 5, 19),
            new ReportedDiagnostic("NOC0001", 10, 55, 10, 63),
        ]);
        buildOutput.DiagnosticsIn("Pages/Index.cshtml").ShouldBe(
        [
            new ReportedDiagnostic("NOC0002", 7, 4, 7, 17),
        ]);
        buildOutput.DiagnosticsIn("Components/MainLayout.razor").ShouldBeEmpty();
        buildOutput.AllResults().Count(r => r.RuleId?.StartsWith("NOC", StringComparison.Ordinal) == true).ShouldBe(4);
    }

    [Fact]
    public async Task NeverReportRazorScaffoldingOrOtherGeneratedCode()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Counter.razor",
            """
            <p>@count</p>

            @code {
                private int count = 1;
            }

            """);
        await project.AddFileAsync("Pages/Index.cshtml", "@page\n<p>Hello</p>\n");
        await project.AddFileAsync(
            "Generated/Widget.g.cs",
            """
            // <auto-generated/>
            namespace Generated;

            /* generated multi-line comment */
            internal static class Widget
            {
                // generated single-line comment
                public static int Size => 1;
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.AllResults().Where(r => r.RuleId?.StartsWith("NOC", StringComparison.Ordinal) == true).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReportCodeBehindFilesLikeAnyCSharpFile()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync("Components/Counter.razor", "<p>@Count</p>\n");
        await project.AddFileAsync(
            "Components/Counter.razor.cs",
            """
            namespace test.Components;

            public partial class Counter
            {
                // in code-behind
                public int Count { get; set; }
            }

            """);
        await project.AddFileAsync("Pages/Index.cshtml", "@page\n@model test.Pages.IndexModel\n<p>Hello</p>\n");
        await project.AddFileAsync(
            "Pages/Index.cshtml.cs",
            """
            namespace test.Pages;

            public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
            {
                /* in page model */
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Counter.razor.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 5, 5, 5, 22),
        ]);
        buildOutput.DiagnosticsIn("Pages/Index.cshtml.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0002", 5, 5, 5, 24),
        ]);
    }
}
