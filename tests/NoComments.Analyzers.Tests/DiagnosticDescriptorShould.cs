using Microsoft.CodeAnalysis;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class DiagnosticDescriptorShould
{
    private const string WhyDescription =
        "Comments drift out of step with the code they describe. Express the intent through names, types and tests instead.";

    private static readonly string[] RulePageSections =
    [
        "## Why",
        "## Examples",
        "## Not reported",
        "## Code fix",
        "## Suppression",
        "## Related rules",
    ];

    public static TheoryData<string> SupportedDiagnosticIds =>
        [.. SupportedDiagnostics().Select(descriptor => descriptor.Id)];

    [Fact]
    public void CoverEveryRule()
    {
        SupportedDiagnostics()
            .Select(descriptor => descriptor.Id)
            .Order(StringComparer.Ordinal)
            .ShouldBe(["NOC0001", "NOC0002", "NOC0003", "NOC0004"]);
    }

    [Theory]
    [InlineData(
        "NOC0001",
        "Do not use single-line comments",
        "Single-line comment: express the intent in code instead",
        WhyDescription)]
    [InlineData(
        "NOC0002",
        "Do not use multi-line comments",
        "Multi-line comment: express the intent in code instead",
        WhyDescription)]
    [InlineData(
        "NOC0003",
        "Do not use documentation comments",
        "Documentation comment: express the intent in code instead",
        WhyDescription + " Only reported when the project generates documentation; otherwise `///` and `/** */` are reported as NOC0001 and NOC0002.")]
    [InlineData(
        "NOC0004",
        "Do not use Razor comments",
        "Razor comment: express the intent in code instead",
        WhyDescription + " Reported at build time only and can only be suppressed project-wide.")]
    public void DescribeItsRuleWithTheSpecifiedText(string id, string title, string message, string description)
    {
        var descriptor = SupportedDiagnostics().Single(d => d.Id == id);

        descriptor.Title.ToString().ShouldBe(title);
        descriptor.MessageFormat.ToString().ShouldBe(message);
        descriptor.Description.ToString().ShouldBe(description);
        descriptor.Category.ShouldBe("Maintainability");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(SupportedDiagnosticIds))]
    public void LinkToItsRulePage(string id)
    {
        var descriptor = SupportedDiagnostics().Single(d => d.Id == id);

        descriptor.HelpLinkUri.ShouldBe($"https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/{id}.md");
        File.Exists(RulePagePath(id)).ShouldBeTrue($"docs/rules/{id}.md is missing");
    }

    [Theory]
    [MemberData(nameof(SupportedDiagnosticIds))]
    public void HaveARulePageWithEverySection(string id)
    {
        var lines = File.ReadAllLines(RulePagePath(id));

        lines[0].ShouldStartWith($"# {id}: ");
        foreach (var section in RulePageSections)
        {
            lines.ShouldContain(section, $"docs/rules/{id}.md has no '{section}' section");
        }
    }

    private static IEnumerable<DiagnosticDescriptor> SupportedDiagnostics() =>
        new CommentAnalyzer().SupportedDiagnostics;

    private static string RulePagePath(string id) =>
        Path.Combine(PathHelpers.GetRootDirectory(), "docs", "rules", $"{id}.md");
}
