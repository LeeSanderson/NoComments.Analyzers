using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class OpinionatedStandardsCompatibilityShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    private const string StandardsPackageId = "Opinionated.DotNet.CodingStandards";
    private const string StandardsPackageVersion = "0.0.22";

    [Fact]
    public async Task BuildAConsumerWithoutCommentsSuccessfully()
    {
        SkipUnlessTheStandardsPackageIsSupported();
        using var project = await CreateStandardsProjectAsync();
        await project.AddFileAsync("Program.cs", "return 0;\n");

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.ExitCode.ShouldBe(0);
        buildOutput.HasError().ShouldBeFalse();
    }

    [Fact]
    public async Task ReportCommentsAsErrorsAndDocumentationCommentsAsNoc0003()
    {
        SkipUnlessTheStandardsPackageIsSupported();
        using var project = await CreateStandardsProjectAsync();
        await project.AddFileAsync("Program.cs", "return Widget.Run();\n");
        await project.AddFileAsync(
            "Widget.cs",
            """
            internal static class Widget
            {
                /// <summary>Runs.</summary>
                /// <returns>The answer.</returns>
                public static int Run()
                {
                    // single
                    return /* multi */ 42;
                }
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.ExitCode.ShouldNotBe(0);
        buildOutput.DiagnosticsIn("Widget.cs").Where(d => d.RuleId.StartsWith("NOC", StringComparison.Ordinal)).ShouldBe(
        [
            new ReportedDiagnostic("NOC0003", 3, 5, 4, 39),
            new ReportedDiagnostic("NOC0001", 7, 9, 7, 18),
            new ReportedDiagnostic("NOC0002", 8, 16, 8, 27),
        ]);
        buildOutput.HasError("NOC0001").ShouldBeTrue();
        buildOutput.HasError("NOC0002").ShouldBeTrue();
        buildOutput.HasError("NOC0003").ShouldBeTrue();
    }

    private static void SkipUnlessTheStandardsPackageIsSupported() =>
        Assert.SkipWhen(ProjectBuilder.ConsumerSdkMajorVersion < 10, "The standards package needs a .NET 10 SDK consumer build");

    private Task<ProjectBuilder> CreateStandardsProjectAsync() =>
        CreateProjectBuilderAsync(packageReferences: [(StandardsPackageId, StandardsPackageVersion)]);
}
