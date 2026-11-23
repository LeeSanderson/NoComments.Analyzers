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
}
