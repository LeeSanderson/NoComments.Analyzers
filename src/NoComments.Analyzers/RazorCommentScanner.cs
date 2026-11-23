using Microsoft.CodeAnalysis.Text;

namespace NoComments.Analyzers;

internal static class RazorCommentScanner
{
    internal static IEnumerable<TextSpan> FindComments(string text)
    {
        var position = 0;
        while (true)
        {
            var start = text.IndexOf("@*", position, StringComparison.Ordinal);
            if (start < 0)
            {
                yield break;
            }

            var close = text.IndexOf("*@", start + 2, StringComparison.Ordinal);
            var end = close < 0 ? text.Length : close + 2;
            yield return TextSpan.FromBounds(start, end);
            position = end;
        }
    }
}
