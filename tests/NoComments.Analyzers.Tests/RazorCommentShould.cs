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
