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
}
