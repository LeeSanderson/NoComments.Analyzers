using Microsoft.CodeAnalysis;

namespace NoComments.Analyzers;

internal static class Rules
{
    private const string Category = "Maintainability";

    private const string WhyDescription =
        "Comments drift out of step with the code they describe. Express the intent through names, types and tests instead.";

    internal static readonly DiagnosticDescriptor SingleLineComment = new(
        id: "NOC0001",
        title: "Do not use single-line comments",
        messageFormat: "Single-line comment: express the intent in code instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: WhyDescription,
        helpLinkUri: HelpLink("NOC0001"));

    internal static readonly DiagnosticDescriptor MultiLineComment = new(
        id: "NOC0002",
        title: "Do not use multi-line comments",
        messageFormat: "Multi-line comment: express the intent in code instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: WhyDescription,
        helpLinkUri: HelpLink("NOC0002"));

    internal static readonly DiagnosticDescriptor DocumentationComment = new(
        id: "NOC0003",
        title: "Do not use documentation comments",
        messageFormat: "Documentation comment: express the intent in code instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: WhyDescription
            + " Only reported when the project generates documentation; otherwise `///` and `/** */` are reported as NOC0001 and NOC0002.",
        helpLinkUri: HelpLink("NOC0003"));

    internal static readonly DiagnosticDescriptor RazorComment = new(
        id: "NOC0004",
        title: "Do not use Razor comments",
        messageFormat: "Razor comment: express the intent in code instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: WhyDescription + " Reported at build time only and can only be suppressed project-wide.",
        helpLinkUri: HelpLink("NOC0004"));

    private static string HelpLink(string id) =>
        $"https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/{id}.md";
}
