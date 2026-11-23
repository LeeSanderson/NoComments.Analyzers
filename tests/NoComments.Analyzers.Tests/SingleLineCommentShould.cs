using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class SingleLineCommentShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task BeReportedOncePerCommentAtItsExactSpan()
    {
        using var project = await CreateProjectBuilderAsync();
        await project.AddFileAsync(
            "Program.cs",
            """
            // first comment
            var answer = 42; // second comment
            System.Console.WriteLine(answer);
                // indented third comment

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 1, 1, 1, 17),
            new ReportedDiagnostic("NOC0001", 2, 18, 2, 35),
            new ReportedDiagnostic("NOC0001", 4, 5, 4, 30),
        ]);
    }

    [Fact]
    public async Task BeDescribedByTheNoc0001Rule()
    {
        using var project = await CreateProjectBuilderAsync();
        await project.AddFileAsync("Program.cs", "return; // comment\n");

        var buildOutput = await project.BuildAndGetOutputAsync();

        var result = buildOutput.AllResults().ShouldHaveSingleItem();
        result.RuleId.ShouldBe("NOC0001");
        result.Level.ShouldBe("warning");
        result.Message.ShouldNotBeNull().Text.ShouldBe("Single-line comment: express the intent in code instead");

        var rule = buildOutput.Rule("NOC0001");
        rule.ShortDescription.ShouldNotBeNull().Text.ShouldBe("Do not use single-line comments");
        rule.FullDescription.ShouldNotBeNull().Text.ShouldBe(
            "Comments drift out of step with the code they describe. Express the intent through names, types and tests instead.");
        rule.Properties.ShouldNotBeNull().Category.ShouldBe("Maintainability");
        rule.DefaultLevel.ShouldBe("warning");
        rule.IsEnabledByDefault.ShouldBeTrue();
        rule.HelpUri.ShouldBe(new Uri("https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0001.md"));
    }
}
