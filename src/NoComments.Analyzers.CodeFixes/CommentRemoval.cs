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
        private readonly List<TextSpan> _deletions = [];
        private readonly List<TextSpan> _joins = [];

        internal IEnumerable<TextChange> Changes()
        {
            var emptied = EmptiedLines();
            for (var i = 0; i < _lines.Length; i++)
            {
                if (emptied[i])
                {
                    _deletions.Add(_lines[i].SpanIncludingLineBreak);
                }
            }

            foreach (var comment in comments.Where(c => !AllLinesEmptied(c, emptied)))
            {
                RemoveAlongsideCode(comment);
            }

            return Merge().Select(span => new TextChange(span, NeedsSpace(span) ? " " : string.Empty));
        }

        private bool[] EmptiedLines()
        {
            var emptied = _lines
                .Select(line => comments.Any(c => Touches(c, line)) && IsBlankOnceRemoved(line.Start, line.End))
                .ToArray();

            foreach (var comment in comments.Where(c => !AllLinesEmptied(c, emptied)))
            {
                for (var i = FirstLine(comment); i <= LastLine(comment); i++)
                {
                    emptied[i] = false;
                }
            }

            return emptied;
        }

        private bool AllLinesEmptied(TextSpan comment, bool[] emptied)
        {
            for (var i = FirstLine(comment); i <= LastLine(comment); i++)
            {
                if (!emptied[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveAlongsideCode(TextSpan comment)
        {
            var firstLine = _lines[FirstLine(comment)];
            var lastLine = _lines[LastLine(comment)];
            var start = comment.Start;
            var end = comment.End;

            if (IsBlankOnceRemoved(firstLine.Start, comment.Start))
            {
                end = SkipSpacesRight(end);
            }
            else if (IsBlankOnceRemoved(comment.End, lastLine.End))
            {
                start = SkipSpacesLeft(start, firstLine.Start);
                end = SkipSpacesRight(end);
            }
            else if (start > firstLine.Start && IsSpace(text[start - 1]))
            {
                start = SkipSpacesLeft(start, firstLine.Start);
            }
            else
            {
                end = SkipSpacesRight(end);
            }

            var deletion = TextSpan.FromBounds(start, end);
            _deletions.Add(deletion);
            _joins.Add(deletion);
        }

        private List<TextSpan> Merge()
        {
            var merged = new List<TextSpan>();
            foreach (var deletion in _deletions.OrderBy(d => d.Start))
            {
                if (merged.Count > 0 && deletion.Start <= merged[merged.Count - 1].End)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = TextSpan.FromBounds(last.Start, Math.Max(last.End, deletion.End));
                }
                else
                {
                    merged.Add(deletion);
                }
            }

            return merged;
        }

        private bool NeedsSpace(TextSpan deletion) =>
            _joins.Exists(deletion.Contains) && IsWordCharacterAt(deletion.Start - 1) && IsWordCharacterAt(deletion.End);

        private bool IsWordCharacterAt(int position) =>
            position >= 0 && position < text.Length && IsWordCharacter(text[position]);

        private int SkipSpacesLeft(int position, int lineStart)
        {
            while (position > lineStart && IsSpace(text[position - 1]))
            {
                position--;
            }

            return position;
        }

        private int SkipSpacesRight(int position)
        {
            while (position < text.Length && IsSpace(text[position]))
            {
                position++;
            }

            return position;
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

        private int FirstLine(TextSpan comment) => text.Lines.IndexOf(comment.Start);

        private int LastLine(TextSpan comment) => text.Lines.IndexOf(Math.Max(comment.Start, comment.End - 1));

        private static bool Touches(TextSpan comment, TextLine line) =>
            comment.Start <= line.End && comment.End > line.Start;

        private static bool IsSpace(char character) => character is ' ' or '\t';

        private static bool IsWordCharacter(char character) => char.IsLetterOrDigit(character) || character == '_';

        private static ImmutableArray<TextLine> ContentLines(SourceText text)
        {
            var lines = text.Lines.ToImmutableArray();
            var last = lines[lines.Length - 1];
            return lines.Length > 1 && last.Start == text.Length ? lines.RemoveAt(lines.Length - 1) : lines;
        }
    }
}
