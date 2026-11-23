using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace NoComments.Analyzers.CodeFixes;

internal static class CommentRemoval
{
    internal static SourceText Remove(SourceText text, ImmutableArray<TextSpan> comments)
    {
        var removal = new Removal(text, comments);
        return text.WithChanges(removal.Changes());
    }

    private sealed class Removal(SourceText text, ImmutableArray<TextSpan> comments)
    {
        private readonly ImmutableArray<TextLine> _lines = ContentLines(text);

        internal IEnumerable<TextChange> Changes()
        {
            var deletions = new List<TextSpan>();
            for (var i = 0; i < _lines.Length; i++)
            {
                var line = _lines[i];
                if (comments.Any(c => Touches(c, line)) && IsBlankOnceRemoved(line.Start, line.End))
                {
                    deletions.Add(line.SpanIncludingLineBreak);
                }
            }

            return deletions.Select(d => new TextChange(d, string.Empty));
        }

        private bool IsBlankOnceRemoved(int start, int end)
        {
            for (var position = start; position < end; position++)
            {
                if (!char.IsWhiteSpace(text[position]) && !IsInComment(position))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInComment(int position) => comments.Any(c => c.Contains(position));

        private static bool Touches(TextSpan comment, TextLine line) =>
            comment.Start <= line.End && comment.End > line.Start;

        private static ImmutableArray<TextLine> ContentLines(SourceText text)
        {
            var lines = text.Lines.ToImmutableArray();
            var last = lines[lines.Length - 1];
            return lines.Length > 1 && last.Start == text.Length ? lines.RemoveAt(lines.Length - 1) : lines;
        }
    }
}
