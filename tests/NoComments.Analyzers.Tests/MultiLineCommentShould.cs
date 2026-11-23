using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class MultiLineCommentShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task BeReportedOncePerCommentAtItsExactSpan()
    {
        using var project = await CreateProjectBuilderAsync();
        await project.AddFileAsync(
            "Program.cs",
            """
            /* first comment */
            var answer = /* second */ 42;
            /*
               third comment
               spanning lines
            */
            System.Console.WriteLine(answer);

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0002", 1, 1, 1, 20),
            new ReportedDiagnostic("NOC0002", 2, 14, 2, 26),
            new ReportedDiagnostic("NOC0002", 3, 1, 6, 3),
        ]);
    }

    [Fact]
    public async Task BeDescribedByTheNoc0002Rule()
    {
        using var project = await CreateProjectBuilderAsync();
        await project.AddFileAsync("Program.cs", "return; /* comment */\n");

        var buildOutput = await project.BuildAndGetOutputAsync();

        var result = buildOutput.AllResults().ShouldHaveSingleItem();
        result.RuleId.ShouldBe("NOC0002");
        result.Level.ShouldBe("warning");
        result.Message.ShouldNotBeNull().Text.ShouldBe("Multi-line comment: express the intent in code instead");

        var rule = buildOutput.Rule("NOC0002");
        rule.ShortDescription.ShouldNotBeNull().Text.ShouldBe("Do not use multi-line comments");
        rule.FullDescription.ShouldNotBeNull().Text.ShouldBe(
            "Comments drift out of step with the code they describe. Express the intent through names, types and tests instead.");
        rule.Properties.ShouldNotBeNull().Category.ShouldBe("Maintainability");
        rule.DefaultLevel.ShouldBe("warning");
        rule.IsEnabledByDefault.ShouldBeTrue();
        rule.HelpUri.ShouldBe(new Uri("https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0002.md"));
    }
}
