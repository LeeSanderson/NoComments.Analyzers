using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class EdgeFormShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    private const string EdgeFormSource =
        """
        return;

        //// four slashes
        internal sealed class Widget
        {
            /**/
            public int Size { get; set; }
        }

        """;

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task BeReportedWhereverRoslynLexesThem(string generateDocumentationFile)
    {
        using var project = await CreateProjectBuilderAsync([("GenerateDocumentationFile", generateDocumentationFile)]);
        await project.AddFileAsync("Program.cs", EdgeFormSource);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 3, 1, 3, 18),
            new ReportedDiagnostic("NOC0002", 6, 5, 6, 9),
        ]);
    }
}
