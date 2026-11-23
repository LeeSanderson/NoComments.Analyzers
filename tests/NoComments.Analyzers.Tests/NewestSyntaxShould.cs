using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class NewestSyntaxShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task NotHideCommentsInsideCSharp12And13Constructs()
    {
        using var project = await CreateProjectBuilderAsync([("LangVersion", "latest")]);
        await project.AddFileAsync(
            "Program.cs",
            """"
            return;

            internal sealed class Widget(int size) /* primary constructor */
            {
                public int[] Items { get; } = [1, /* collection expression */ 2];

                public string Describe() => $$"""
                    {{size /* inside raw interpolation */}}
                    """;

                public static int Sum(params ReadOnlySpan<int> values) // params collection
                {
                    var lockObject = new System.Threading.Lock(); /* lock type */
                    lock (lockObject)
                    {
                        return values.Length;
                    }
                }
            }

            """");

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0002", 3, 40, 3, 65),
            new ReportedDiagnostic("NOC0002", 5, 39, 5, 66),
            new ReportedDiagnostic("NOC0002", 8, 16, 8, 46),
            new ReportedDiagnostic("NOC0001", 11, 60, 11, 80),
            new ReportedDiagnostic("NOC0002", 13, 55, 13, 70),
        ]);
    }

    [Fact]
    public async Task NotHideCommentsInsideCSharp14Constructs()
    {
        Assert.SkipWhen(ProjectBuilder.ConsumerSdkMajorVersion < 10, "C# 14 needs a .NET 10 SDK consumer build");

        using var project = await CreateProjectBuilderAsync([("LangVersion", "latest")]);
        await project.AddFileAsync(
            "Program.cs",
            """
            return;

            internal static class Extensions
            {
                extension(string text) // on extension block
                {
                    public bool IsBlank => /* in extension member */ text.Length == 0;
                }
            }

            internal sealed class Widget
            {
                public int Size
                {
                    get => field; // field keyword
                    set => field = value;
                }

                public int[] Items { get; } = [1, 2];

                public static void Reset(Widget? widget)
                {
                    widget?.Items[0] = 0; // null-conditional assignment
                }
            }

            """);

        var buildOutput = await project.BuildAndGetOutputAsync();

        buildOutput.DiagnosticsIn("Program.cs").ShouldBe(
        [
            new ReportedDiagnostic("NOC0001", 5, 28, 5, 49),
            new ReportedDiagnostic("NOC0002", 7, 32, 7, 57),
            new ReportedDiagnostic("NOC0001", 15, 23, 15, 39),
            new ReportedDiagnostic("NOC0001", 23, 31, 23, 61),
        ]);
    }
}
