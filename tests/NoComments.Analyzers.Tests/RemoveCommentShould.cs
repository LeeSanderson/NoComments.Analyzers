using Microsoft.CodeAnalysis;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RemoveCommentShould
{
    public static TheoryData<string, string, DocumentationMode> WalkthroughCases()
    {
        var cases = new TheoryData<string, string, DocumentationMode>();
        foreach (var walkthrough in Walkthroughs.All.Keys)
        {
            foreach (var lineEnding in new[] { "LF", "CRLF" })
            {
                cases.Add(walkthrough, lineEnding, DocumentationMode.Diagnose);
                cases.Add(walkthrough, lineEnding, DocumentationMode.None);
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(WalkthroughCases))]
    public async Task GiveTheWalkthroughResultWhenFixedOneCommentAtATime(
        string walkthrough,
        string lineEnding,
        DocumentationMode documentationMode)
    {
        var (before, after) = Walkthroughs.All[walkthrough];

        var result = await SingleFixHarness.ApplySingleFixesUntilNoneRemainAsync(
            WithLineEndings(before, lineEnding),
            documentationMode);

        result.ShouldBe(WithLineEndings(after, lineEnding));
    }

    private static string WithLineEndings(string text, string lineEnding) =>
        text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", lineEnding == "CRLF" ? "\r\n" : "\n", StringComparison.Ordinal);
}
