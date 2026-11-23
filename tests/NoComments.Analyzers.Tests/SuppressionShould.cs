using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class SuppressionShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    private const string CommentedWidget =
        """
        internal sealed class Widget
        {
            /// <summary>Runs.</summary>
            public static int Run()
            {
                // single
                return /* multi */ 42;
            }
        }

        """;

    private static readonly ReportedDiagnostic[] UnsuppressedDiagnostics =
    [
        new("NOC0003", 3, 5, 3, 33),
        new("NOC0001", 6, 9, 6, 18),
        new("NOC0002", 7, 16, 7, 27),
    ];

    [Fact]
    public async Task LeaveEveryRuleReportedWithoutASuppression()
    {
        using var project = await CreateCommentedProjectAsync(CommentedWidget);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Widget.cs").ShouldBe(UnsuppressedDiagnostics, ignoreOrder: true);
    }

    [Fact]
    public async Task SilenceEveryRuleWithPragmaWarningDisable()
    {
        using var project = await CreateCommentedProjectAsync(
            "#pragma warning disable NOC0001, NOC0002, NOC0003\n" + CommentedWidget);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Widget.cs").ShouldBeEmpty();
    }

    [Fact]
    public async Task SilenceEveryRuleWithSuppressMessageOnTheEnclosingType()
    {
        using var project = await CreateCommentedProjectAsync(
            """
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "NOC0001")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "NOC0002")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "NOC0003")]

            """ + CommentedWidget);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Widget.cs").ShouldBeEmpty();
    }

    [Fact]
    public async Task SilenceEveryRuleWithGlobalSuppressions()
    {
        using var project = await CreateCommentedProjectAsync(CommentedWidget);
        await project.AddFileAsync(
            "GlobalSuppressions.cs",
            """
            using System.Diagnostics.CodeAnalysis;

            [assembly: SuppressMessage("Maintainability", "NOC0001", Scope = "type", Target = "~T:Widget")]
            [assembly: SuppressMessage("Maintainability", "NOC0002", Scope = "type", Target = "~T:Widget")]
            [assembly: SuppressMessage("Maintainability", "NOC0003", Scope = "type", Target = "~T:Widget")]

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Widget.cs").ShouldBeEmpty();
    }

    [Fact]
    public async Task SilenceEveryRuleWithAnEditorConfigSeverityOfNone()
    {
        using var project = await CreateCommentedProjectAsync(CommentedWidget);
        await project.AddFileAsync(
            ".editorconfig",
            """
            root = true

            [*.cs]
            dotnet_diagnostic.NOC0001.severity = none
            dotnet_diagnostic.NOC0002.severity = none
            dotnet_diagnostic.NOC0003.severity = none

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.AllResults().ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyADifferentEditorConfigSeverityToEachRule()
    {
        using var project = await CreateCommentedProjectAsync(CommentedWidget);
        await project.AddFileAsync(
            ".editorconfig",
            """
            root = true

            [*.cs]
            dotnet_diagnostic.NOC0001.severity = error
            dotnet_diagnostic.NOC0002.severity = suggestion
            dotnet_diagnostic.NOC0003.severity = none

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Widget.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 6, 9, 6, 18),
            new ReportedDiagnostic("NOC0002", 7, 16, 7, 27),
        ]);
        buildOutput.HasError("NOC0001").ShouldBeTrue();
        buildOutput.HasNote("NOC0002").ShouldBeTrue();
    }

    private async Task<ProjectBuilder> CreateCommentedProjectAsync(string widgetSource)
    {
        var project = await CreateProjectBuilderAsync([("GenerateDocumentationFile", "true"), ("NoWarn", "$(NoWarn);CS1591")]);
        await project.AddFileAsync("Program.cs", "return Widget.Run();\n");
        await project.AddFileAsync("Widget.cs", widgetSource);
        return project;
    }
}
