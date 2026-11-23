using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RazorCommentSuppressionShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    private const string CommentedComponentPath = "Components/Hello.razor";

    private const string CommentedComponent =
        """
        <h1>Hello</h1>
        @* greeting *@

        """;

    private static readonly ReportedDiagnostic[] UnsuppressedDiagnostics =
    [
        new("NOC0004", 2, 1, 2, 15),
    ];

    [Fact]
    public async Task SilenceNoc0004WithAGlobalConfigSeverityOfNone()
    {
        using var project = await CreateCommentedProjectAsync();
        await project.AddFileAsync(
            ".globalconfig",
            """
            is_global = true

            dotnet_diagnostic.NOC0004.severity = none

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.AllResults().ShouldBeEmpty();
    }

    [Fact]
    public async Task SilenceNoc0004WithNoWarn()
    {
        using var project = await CreateCommentedProjectAsync([("NoWarn", "$(NoWarn);NOC0004")]);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.AllResults().ShouldBeEmpty();
    }

    [Fact]
    public async Task SilenceNoc0004WithAnAssemblyLevelSuppressMessageWithNoTarget()
    {
        using var project = await CreateCommentedProjectAsync();
        await project.AddFileAsync(
            "GlobalSuppressions.cs",
            """
            [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "NOC0004")]

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn(CommentedComponentPath).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReportNoc0004AsAnErrorWithTreatWarningsAsErrors()
    {
        using var project = await CreateCommentedProjectAsync([("TreatWarningsAsErrors", "true")]);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn(CommentedComponentPath).ShouldBe(UnsuppressedDiagnostics);
        buildOutput.HasError("NOC0004").ShouldBeTrue();
        buildOutput.ExitCode.ShouldNotBe(0);
    }

    [Fact]
    public async Task ReportNoc0004AsAnErrorWithWarningsAsErrors()
    {
        using var project = await CreateCommentedProjectAsync([("WarningsAsErrors", "$(WarningsAsErrors);NOC0004")]);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn(CommentedComponentPath).ShouldBe(UnsuppressedDiagnostics);
        buildOutput.HasError("NOC0004").ShouldBeTrue();
        buildOutput.ExitCode.ShouldNotBe(0);
    }

    [Fact]
    public async Task IgnoreAnEditorConfigSeverityForNoc0004()
    {
        using var project = await CreateCommentedProjectAsync();
        await project.AddFileAsync(
            ".editorconfig",
            """
            root = true

            [*]
            dotnet_diagnostic.NOC0004.severity = none

            [*.razor]
            dotnet_diagnostic.NOC0004.severity = none

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn(CommentedComponentPath).ShouldBe(UnsuppressedDiagnostics);
        buildOutput.HasWarning("NOC0004").ShouldBeTrue();
    }

    [Fact]
    public async Task IgnorePragmaWarningDisableForNoc0004()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync("Pragma.cs", "#pragma warning disable NOC0004\nnamespace test;\n");
        await project.AddFileAsync(
            CommentedComponentPath,
            """
            @code {
            #pragma warning disable NOC0004
            }
            @* greeting *@

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn(CommentedComponentPath).ShouldBe(
        [
            new ReportedDiagnostic("NOC0004", 4, 1, 4, 15),
        ]);
    }

    private async Task<ProjectBuilder> CreateCommentedProjectAsync((string Name, string Value)[]? properties = null)
    {
        var project = await CreateRazorProjectBuilderAsync(properties);
        await project.AddFileAsync(CommentedComponentPath, CommentedComponent);
        return project;
    }
}
