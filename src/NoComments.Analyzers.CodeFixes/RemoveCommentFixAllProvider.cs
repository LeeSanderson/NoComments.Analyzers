using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace NoComments.Analyzers.CodeFixes;

internal sealed class RemoveCommentFixAllProvider : FixAllProvider
{
    internal static readonly RemoveCommentFixAllProvider Instance = new();

    private RemoveCommentFixAllProvider()
    {
    }

    public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() =>
        [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        var diagnostics = await DiagnosticsInScopeAsync(fixAllContext).ConfigureAwait(false);
        return diagnostics.IsEmpty ? null : RemoveCommentAction.Create(fixAllContext.Solution, diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> DiagnosticsInScopeAsync(FixAllContext fixAllContext)
    {
        switch (fixAllContext.Scope)
        {
            case FixAllScope.Document when fixAllContext.Document is { } document:
                return await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            case FixAllScope.Project:
                return await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false);
            case FixAllScope.Solution:
                var all = ImmutableArray.CreateBuilder<Diagnostic>();
                foreach (var project in fixAllContext.Solution.Projects)
                {
                    all.AddRange(await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false));
                }

                return all.ToImmutable();
            default:
                return ImmutableArray<Diagnostic>.Empty;
        }
    }
}
