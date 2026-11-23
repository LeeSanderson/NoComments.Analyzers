using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class DotnetFormatRazorShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    private static readonly string[] CSharpCommentIds = ["NOC0001", "NOC0002", "NOC0003"];

    [Fact]
    public async Task RemoveCSharpCommentsFromACodeBlockInAComponent()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Counter.razor",
            WithLf(
                """
                <h1>Counter</h1>

                @code {
                    // current value
                    private int count; // starts at zero

                    private void Up() => count++; /* bump */
                }

                """));

        var exitCode = await project.FormatAnalyzersAsync(CSharpCommentIds, includeGenerated: true);

        exitCode.ShouldBe(0);
        (await project.ReadFileAsync("Components/Counter.razor")).ShouldBe(WithLf(
            """
            <h1>Counter</h1>

            @code {
                private int count;

                private void Up() => count++;
            }

            """));
    }

    [Fact]
    public async Task RemoveCSharpCommentsFromAPageAndKeepRazorComments()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Pages/Index.cshtml",
            WithLf(
                """
                @page
                @{
                    var total = 3; // three
                    var/* kind */label = "items";
                }
                @* Page header *@
                <h1>Hello</h1>
                <p>Total: @total @* not formatted yet *@ @label</p>
                <p>Joined@* x *@text @(total /* in an expression */)</p>

                """));

        var exitCode = await project.FormatAnalyzersAsync(CSharpCommentIds, includeGenerated: true);

        exitCode.ShouldBe(0);
        (await project.ReadFileAsync("Pages/Index.cshtml")).ShouldBe(WithLf(
            """
            @page
            @{
                var total = 3;
                var label = "items";
            }
            @* Page header *@
            <h1>Hello</h1>
            <p>Total: @total @* not formatted yet *@ @label</p>
            <p>Joined@* x *@text @(total)</p>

            """));
    }

    [Fact]
    public async Task FixCSharpAndRazorFilesInOneRun()
    {
        using var project = await CreateRazorProjectBuilderAsync();
        await project.AddFileAsync(
            "Components/Counter.razor",
            WithLf(
                """
                <p>@count</p>

                @code {
                    // the count
                    private int count = Settings.Start; // from settings
                }

                """));
        await project.AddFileAsync(
            "Pages/Index.cshtml",
            WithLf(
                """
                @page
                @{
                    // nothing to set up
                }
                <p>Hello</p>

                """));
        await project.AddFileAsync(
            "Settings.cs",
            WithLf(
                """
                // shared settings
                internal static class Settings
                {
                    internal const int Start = 1; // first value
                }

                """));

        var exitCode = await project.FormatAnalyzersAsync(["NOC0001"], includeGenerated: true);

        exitCode.ShouldBe(0);
        (await project.ReadFileAsync("Components/Counter.razor")).ShouldBe(WithLf(
            """
            <p>@count</p>

            @code {
                private int count = Settings.Start;
            }

            """));
        (await project.ReadFileAsync("Pages/Index.cshtml")).ShouldBe(WithLf(
            """
            @page
            @{
            }
            <p>Hello</p>

            """));
        (await project.ReadFileAsync("Settings.cs")).ShouldBe(WithLf(
            """
            internal static class Settings
            {
                internal const int Start = 1;
            }

            """));
    }

    [Fact]
    public async Task NeverEditTheGeneratedCode()
    {
        using var project = await CreateRazorProjectBuilderAsync([("EmitCompilerGeneratedFiles", "true")]);
        await project.AddFileAsync(
            "Components/Counter.razor",
            WithLf(
                """
                <p>@count</p>

                @code {
                    private int count = 1; // one
                }

                """));
        await project.BuildAndGetOutputAsync();
        var generatedBefore = GeneratedFilesIn(project);
        generatedBefore.ShouldContain(file => file.Path.EndsWith("Counter_razor.g.cs", StringComparison.Ordinal));

        var exitCode = await project.FormatAnalyzersAsync(CSharpCommentIds, includeGenerated: true);

        exitCode.ShouldBe(0);
        GeneratedFilesIn(project).ShouldBe(generatedBefore);
        (await project.ReadFileAsync("Components/Counter.razor")).ShouldBe(WithLf(
            """
            <p>@count</p>

            @code {
                private int count = 1;
            }

            """));
    }

    private static (string Path, string Content)[] GeneratedFilesIn(ProjectBuilder project) =>
        Directory.EnumerateFiles(project.RootFolder, "*.g.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (path, File.ReadAllText(path)))
            .ToArray();

    private static string WithLf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
