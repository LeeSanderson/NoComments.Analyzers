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

    private static string HelpLink(string id) =>
        $"https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/{id}.md";
}
