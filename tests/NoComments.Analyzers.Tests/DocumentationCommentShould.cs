using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class DocumentationCommentShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    private const string DocumentedSource =
        """
        return;

        /// <summary>
        /// A widget.
        /// </summary>
        internal sealed class Widget
        {
            /** <summary>Size.</summary> */
            public int Size { get; set; }

            /**
             * <summary>Colour.</summary>
             */
            public string? Colour { get; set; }
        }

        """;

    [Fact]
    public async Task BeReportedOncePerCommentWhenTheProjectGeneratesDocumentation()
    {
        using var project = await CreateProjectBuilderAsync([("GenerateDocumentationFile", "true")]);
        await project.AddFileAsync("Program.cs", DocumentedSource);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0003", 3, 1, 5, 15),
            new ReportedDiagnostic("NOC0003", 8, 5, 8, 36),
            new ReportedDiagnostic("NOC0003", 11, 5, 13, 8),
        ]);
    }

    [Fact]
    public async Task BeReportedAsSingleAndMultiLineCommentsWhenTheProjectDoesNotGenerateDocumentation()
    {
        using var project = await CreateProjectBuilderAsync([("GenerateDocumentationFile", "false")]);
        await project.AddFileAsync("Program.cs", DocumentedSource);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 3, 1, 3, 14),
            new ReportedDiagnostic("NOC0001", 4, 1, 4, 14),
            new ReportedDiagnostic("NOC0001", 5, 1, 5, 15),
            new ReportedDiagnostic("NOC0002", 8, 5, 8, 36),
            new ReportedDiagnostic("NOC0002", 11, 5, 13, 8),
        ]);
    }
}
