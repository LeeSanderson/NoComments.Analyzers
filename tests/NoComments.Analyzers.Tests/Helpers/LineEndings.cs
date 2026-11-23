namespace NoComments.Analyzers.Tests.Helpers;

internal static class LineEndings
{
    internal static string WithLf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
