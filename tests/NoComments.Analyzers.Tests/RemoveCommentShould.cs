using Microsoft.CodeAnalysis;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RemoveCommentShould
{
    public static TheoryData<string> WalkthroughNames => [.. Walkthroughs.All.Keys];

    [Theory]
    [MemberData(nameof(WalkthroughNames))]
    public async Task GiveTheWalkthroughResultWhenFixedOneCommentAtATime(string walkthrough)
    {
        var (before, after) = Walkthroughs.All[walkthrough];

        var result = await SingleFixHarness.ApplySingleFixesUntilNoneRemainAsync(Lf(before), DocumentationMode.Diagnose);

        result.ShouldBe(Lf(after));
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
