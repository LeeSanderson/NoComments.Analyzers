using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RazorCommentShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task ReportARazorCommentInMarkupAsNoc0004()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Hello.razor",
            """
            <h1>Hello</h1>
              @* greeting *@
            <p>World</p>

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Hello.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0004", 2, 3, 2, 17),
        ]);
        buildOutput.HasWarning("NOC0004").ShouldBeTrue();
    }

    [Fact]
    public async Task ReportRazorCommentsInACodeBlockButNotLookAlikesInAComponent()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Widget.razor",
            """"
            @* top *@
            <p>@Text @Path @Raw @Mixed @At @Quote</p>
            <!-- an HTML comment -->

            @code {
                @* inside code *@
                private string Text { get; } = "not @* a comment *@";
                private string Path { get; } = @"C:\@*\*@";
                private string Raw { get; } = """still @* not "" a comment *@""";
                private string Mixed => $"{Text} @* {"x"} *@";
                // single @* *@
                /* multi @* *@ */
                private char At { get; } = '@';
                private char Quote { get; } = '"';
                @* after strings *@
            }

            """");

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/Widget.razor").Where(d => d.RuleId == "NOC0004").ShouldBe(
        [
            new ReportedDiagnostic("NOC0004", 1, 1, 1, 10),
            new ReportedDiagnostic("NOC0004", 6, 5, 6, 22),
            new ReportedDiagnostic("NOC0004", 15, 5, 15, 24),
        ]);
    }

    [Fact]
    public async Task ReportRazorCommentsInMarkupAndCodeButNotLookAlikesInAPage()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Pages/Index.cshtml",
            """
            @page
            @{
                var text = "@* not a comment *@";
                <p>Don't @* in markup *@ stop</p>
                @* in a code block *@
            }
            <p>@text @Verbatim</p>
            <!-- an HTML comment -->
            @functions {
                private static string Verbatim => @"@* not *@";
            }
            @* at the end *@

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Pages/Index.cshtml").ShouldBe(
        [
            new ReportedDiagnostic("NOC0004", 4, 14, 4, 29),
            new ReportedDiagnostic("NOC0004", 5, 5, 5, 26),
            new ReportedDiagnostic("NOC0004", 12, 1, 12, 17),
        ]);
    }

    [Fact]
    public async Task FollowMarkupInsideControlBlocks()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/List.razor",
            """
            @if (DateTime.Now.Year > 2000)
            {
                <p>It's @* in if *@ here</p>
            }
            else
            {
                <p>Isn't "it"</p>
            }
            @foreach (var item in new[] { "a@*", "b" })
            {
                <span title="@item's">@item</span><br>
            }
            @* after *@

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Components/List.razor").ShouldBe(
        [
            new ReportedDiagnostic("NOC0004", 3, 13, 3, 24),
            new ReportedDiagnostic("NOC0004", 13, 1, 13, 12),
        ]);
    }

    [Fact]
    public async Task NeverReportHtmlComments()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync("Components/Hello.razor", "<!-- hello -->\n<p>Hello</p>\n");
        await project.AddFileAsync("Pages/Index.cshtml", "@page\n<!--\n  multi-line\n-->\n<p>Hello</p>\n");

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.AllResults().Where(r => r.RuleId?.StartsWith("NOC", StringComparison.Ordinal) == true).ShouldBeEmpty();
    }

    [Fact]
    public async Task DescribeNoc0004WithTheSpecifiedText()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync("Components/Hello.razor", "@* greeting *@\n");

        var buildOutput = await project.BuildAndGetOutputAsync();

        var rule = buildOutput.Rule("NOC0004");
        rule.ShortDescription.ShouldNotBeNull().Text.ShouldBe("Do not use Razor comments");
        rule.FullDescription.ShouldNotBeNull().Text.ShouldBe(
            "Comments drift out of step with the code they describe. Express the intent through names, types and tests instead. Reported at build time only and can only be suppressed project-wide.");
        rule.Properties.ShouldNotBeNull().Category.ShouldBe("Maintainability");
        rule.DefaultLevel.ShouldBe("warning");
        rule.IsEnabledByDefault.ShouldBeTrue();
        rule.HelpUri.ShouldBe(new Uri("https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0004.md"));
        buildOutput.AllResults().Single(r => r.RuleId == "NOC0004").Message.ShouldNotBeNull().Text
            .ShouldBe("Razor comment: express the intent in code instead");
    }
}
