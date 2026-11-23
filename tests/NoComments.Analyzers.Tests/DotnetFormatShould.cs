using NoComments.Analyzers.Tests.Helpers;
using Shouldly;
using static NoComments.Analyzers.Tests.Helpers.LineEndings;

namespace NoComments.Analyzers.Tests;

public class DotnetFormatShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    public static TheoryData<string, bool, string, string> Samples() => new()
    {
        {
            "NOC0001",
            false,
            """
            // Greets the user
            Console.WriteLine("Hello"); // says hello
            /* kept: not NOC0001 */
            var total = 1 /* also kept */ + 2;
            Console.WriteLine(total);

            """,
            """
            Console.WriteLine("Hello");
            /* kept: not NOC0001 */
            var total = 1 /* also kept */ + 2;
            Console.WriteLine(total);

            """
        },
        {
            "NOC0002",
            false,
            """
            /*
             * Greets the user
             */
            Console.WriteLine("Hello"); // kept: not NOC0002
            var total = 1 /* one */ + 2;
            Console.WriteLine(total);

            """,
            """
            Console.WriteLine("Hello"); // kept: not NOC0002
            var total = 1 + 2;
            Console.WriteLine(total);

            """
        },
        {
            "NOC0003",
            true,
            """
            Console.WriteLine(new Widget().Size);

            /// <summary>
            /// A widget.
            /// </summary>
            internal sealed class Widget
            {
                /** <summary>Size.</summary> */
                public int Size { get; set; } // kept: not NOC0003
            }

            """,
            """
            Console.WriteLine(new Widget().Size);

            internal sealed class Widget
            {
                public int Size { get; set; } // kept: not NOC0003
            }

            """
        },
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task RemoveTheCommentsOfTheRequestedDiagnostic(
        string diagnosticId,
        bool generateDocumentation,
        string before,
        string after)
    {
        using var project = await CreateProjectBuilderAsync(
            [("GenerateDocumentationFile", generateDocumentation ? "true" : "false")]);
        await project.AddFileAsync("Program.cs", WithLf(before));

        var exitCode = await project.FormatAnalyzersAsync(diagnosticId);

        exitCode.ShouldBe(0);
        (await project.ReadFileAsync("Program.cs")).ShouldBe(WithLf(after));
    }
}
