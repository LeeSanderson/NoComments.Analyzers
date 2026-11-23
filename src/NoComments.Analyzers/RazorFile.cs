using Microsoft.CodeAnalysis;

namespace NoComments.Analyzers;

internal static class RazorFile
{
    internal static bool HasRazorExtension(string path) =>
        path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSourceOf(Location location)
    {
        var mapped = location.GetMappedLineSpan();
        return mapped.HasMappedPath && HasRazorExtension(mapped.Path);
    }
}
