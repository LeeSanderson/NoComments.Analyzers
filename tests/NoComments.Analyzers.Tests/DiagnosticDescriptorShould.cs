using Microsoft.CodeAnalysis;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class DiagnosticDescriptorShould
{
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
