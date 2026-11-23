using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RemoveCommentShould
{
    private const string RemoveCommentEquivalenceKey = "Remove comment";

    private static readonly string[] LineEndings = ["LF", "CRLF"];
    private static readonly DocumentationMode[] DocumentationModes = [DocumentationMode.Diagnose, DocumentationMode.None];
    private static readonly FixAllScope[] FixAllScopes = [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

    public static TheoryData<string, string, DocumentationMode> WalkthroughCases()
    {
        var cases = new TheoryData<string, string, DocumentationMode>();
        var combinations =
            from walkthrough in Walkthroughs.All.Keys
            from lineEnding in LineEndings
            from documentationMode in DocumentationModes
            select (walkthrough, lineEnding, documentationMode);
        foreach (var (walkthrough, lineEnding, documentationMode) in combinations)
        {
            cases.Add(walkthrough, lineEnding, documentationMode);
        }

        return cases;
    }

    public static TheoryData<string, string, DocumentationMode, FixAllScope> FixAllCases()
    {
        var cases = new TheoryData<string, string, DocumentationMode, FixAllScope>();
        var combinations =
            from walkthrough in Walkthroughs.All.Keys
            from lineEnding in LineEndings
            from documentationMode in DocumentationModes
            from scope in FixAllScopes
            select (walkthrough, lineEnding, documentationMode, scope);
        foreach (var (walkthrough, lineEnding, documentationMode, scope) in combinations)
        {
            cases.Add(walkthrough, lineEnding, documentationMode, scope);
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

    [Theory]
    [MemberData(nameof(FixAllCases))]
    public async Task GiveTheSameResultWithFixAllAsWhenFixedOneCommentAtATime(
        string walkthrough,
        string lineEnding,
        DocumentationMode documentationMode,
        FixAllScope scope)
    {
        var source = WithLineEndings(Walkthroughs.All[walkthrough].Before, lineEnding);
        var oneAtATime = await SingleFixHarness.ApplySingleFixesUntilNoneRemainAsync(source, documentationMode);

        var fixAll = await SingleFixHarness.ApplyFixAllAsync(source, documentationMode, scope, RemoveCommentEquivalenceKey);

        fixAll.ShouldBe(oneAtATime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Some other fix")]
    public async Task RemoveEveryCommentWithFixAllWhateverTheEquivalenceKey(string? equivalenceKey)
    {
        var (before, after) = Walkthroughs.All["10 Two comments on one line"];

        var fixAll = await SingleFixHarness.ApplyFixAllAsync(
            WithLineEndings(before, "LF"),
            DocumentationMode.Diagnose,
            FixAllScope.Document,
            equivalenceKey);

        fixAll.ShouldBe(WithLineEndings(after, "LF"));
    }

    private static string WithLineEndings(string text, string lineEnding) =>
        text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", lineEnding == "CRLF" ? "\r\n" : "\n", StringComparison.Ordinal);
}
