using Microsoft.CodeAnalysis.Text;

namespace NoComments.Analyzers;

internal sealed class RazorCommentScanner
{
    private static readonly HashSet<string> BlockKeywords = new(StringComparer.Ordinal)
    {
        "code", "functions", "if", "for", "foreach", "while", "switch", "lock", "try", "do", "section",
    };

    private static readonly HashSet<string> ContinuationKeywords = new(StringComparer.Ordinal)
    {
        "else", "catch", "finally",
    };

    private static readonly HashSet<string> LineDirectives = new(StringComparer.Ordinal)
    {
        "page", "model", "inject", "inherits", "attribute", "implements", "layout", "namespace", "typeparam",
        "rendermode", "preservewhitespace", "addTagHelper", "removeTagHelper", "tagHelperPrefix",
    };

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr",
    };

    private const int PairLength = 2;
    private const int MinimumRawQuoteCount = 3;

    private readonly string _text;
    private readonly List<TextSpan> _comments = [];
    private int _position;

    private RazorCommentScanner(string text)
    {
        _text = text;
    }

    private bool AtEnd => _position >= _text.Length;

    private char Current => Peek(0);

    internal static IReadOnlyList<TextSpan> FindComments(string text)
    {
        var scanner = new RazorCommentScanner(text);
        scanner.ScanMarkup();
        return scanner._comments;
    }

    private char Peek(int offset) =>
        _position + offset < _text.Length ? _text[_position + offset] : '\0';

    private bool StartsWith(string value) =>
        string.CompareOrdinal(_text, _position, value, 0, value.Length) == 0;

    private void ScanMarkup()
    {
        while (!AtEnd)
        {
            if (Current == '@')
            {
                ScanTransition();
            }
            else
            {
                _position++;
            }
        }
    }

    private void ScanTransition()
    {
        var next = Peek(1);
        if (next == '*')
        {
            ScanRazorComment();
            return;
        }

        if (IsEmailAddress() || next == '@')
        {
            _position += PairLength;
            return;
        }

        _position++;
        switch (next)
        {
            case '{':
                _position++;
                ScanCode('}', '{');
                break;
            case '(':
                _position++;
                ScanCode(')', '(');
                break;
            default:
                if (IsIdentifierStart(next))
                {
                    ScanKeywordOrExpression(ReadIdentifier());
                }

                break;
        }
    }

    private bool IsEmailAddress() =>
        _position > 0 && char.IsLetterOrDigit(_text[_position - 1]) && char.IsLetterOrDigit(Peek(1));

    private void ScanKeywordOrExpression(string word)
    {
        if (BlockKeywords.Contains(word))
        {
            ScanBlockStatement();
        }
        else if (word == "using")
        {
            ScanUsing();
        }
        else if (LineDirectives.Contains(word))
        {
            ScanDirectiveLine();
        }
        else
        {
            ScanImplicitExpression();
        }
    }

    private void ScanUsing()
    {
        var start = _position;
        SkipWhitespace();
        var isBlock = Current == '(';
        _position = start;
        if (isBlock)
        {
            ScanBlockStatement();
        }
        else
        {
            ScanDirectiveLine();
        }
    }

    private void ScanBlockStatement()
    {
        while (!AtEnd)
        {
            if (Current == '{')
            {
                _position++;
                ScanCode('}', '{');
                if (!TryContinueStatement())
                {
                    return;
                }
            }
            else
            {
                SkipCodeElementOrCharacter();
            }
        }
    }

    private void SkipCodeElementOrCharacter()
    {
        if (!TryScanCodeElement())
        {
            _position++;
        }
    }

    private bool TryContinueStatement()
    {
        var start = _position;
        SkipWhitespace();
        if (IsIdentifierStart(Current) && ContinuationKeywords.Contains(ReadIdentifier()))
        {
            return true;
        }

        _position = start;
        return false;
    }

    private void ScanDirectiveLine()
    {
        while (!AtEnd && Current != '\n')
        {
            SkipCodeElementOrCharacter();
        }
    }

    private void ScanImplicitExpression()
    {
        while (!AtEnd)
        {
            if (Current == '(')
            {
                _position++;
                ScanCode(')', '(');
            }
            else if (Current == '[')
            {
                _position++;
                ScanCode(']', '[');
            }
            else if (Current == '.' && IsIdentifierStart(Peek(1)))
            {
                _position++;
                ReadIdentifier();
            }
            else
            {
                return;
            }
        }
    }

    private void ScanCode(char closer, char opener)
    {
        var lastSignificant = opener;
        while (!AtEnd)
        {
            var current = Current;
            if (current == closer)
            {
                _position++;
                return;
            }

            if (TryScanNested(current))
            {
                lastSignificant = current;
            }
            else if (TryScanCodeTransition() || TryScanMarkupStatement(lastSignificant))
            {
                lastSignificant = ';';
            }
            else if (TryScanCodeElement())
            {
                lastSignificant = '"';
            }
            else
            {
                lastSignificant = char.IsWhiteSpace(current) ? lastSignificant : current;
                _position++;
            }
        }
    }

    private bool TryScanNested(char current)
    {
        var closer = current switch
        {
            '{' => '}',
            '(' => ')',
            '[' => ']',
            _ => '\0',
        };
        if (closer == '\0')
        {
            return false;
        }

        _position++;
        ScanCode(closer, current);
        return true;
    }

    private bool TryScanCodeTransition()
    {
        if (Current != '@')
        {
            return false;
        }

        switch (Peek(1))
        {
            case ':':
                _position += PairLength;
                ScanMarkupLine();
                return true;
            case '<':
                _position++;
                ScanMarkupElement();
                return true;
            default:
                return false;
        }
    }

    private bool TryScanMarkupStatement(char lastSignificant)
    {
        if (Current != '<' || !char.IsLetter(Peek(1)) || lastSignificant is not ('{' or '}' or ';' or ':'))
        {
            return false;
        }

        ScanMarkupElement();
        return true;
    }

    private bool TryScanCodeElement()
    {
        if (Current == '@' && Peek(1) == '*')
        {
            ScanRazorComment();
            return true;
        }

        if (StartsWith("//"))
        {
            SkipToEndOfLine();
            return true;
        }

        if (StartsWith("/*"))
        {
            SkipPast("*/");
            return true;
        }

        if (Current == '\'')
        {
            ScanCharacterLiteral();
            return true;
        }

        return TryScanStringLiteral();
    }

    private void ScanCharacterLiteral()
    {
        _position++;
        while (!AtEnd && Current != '\'' && Current != '\n')
        {
            _position += Current == '\\' ? PairLength : 1;
        }

        _position++;
    }

    private bool TryScanStringLiteral()
    {
        var index = _position;
        var isInterpolated = false;
        var isVerbatim = false;
        while (index < _text.Length && _text[index] is '$' or '@')
        {
            isInterpolated |= _text[index] == '$';
            isVerbatim |= _text[index] == '@';
            index++;
        }

        if (index >= _text.Length || _text[index] != '"')
        {
            return false;
        }

        _position = index;
        var quotes = CountQuotes();
        if (!isVerbatim && quotes >= MinimumRawQuoteCount)
        {
            _position += quotes;
            SkipPast(new string('"', quotes));
        }
        else
        {
            _position++;
            ScanQuotedString(isVerbatim, isInterpolated);
        }

        return true;
    }

    private int CountQuotes()
    {
        var count = 0;
        while (Peek(count) == '"')
        {
            count++;
        }

        return count;
    }

    private void ScanQuotedString(bool isVerbatim, bool isInterpolated)
    {
        var hasEnded = false;
        while (!AtEnd && !hasEnded)
        {
            hasEnded = ScanStringCharacter(isVerbatim, isInterpolated);
        }
    }

    private bool ScanStringCharacter(bool isVerbatim, bool isInterpolated)
    {
        var current = Current;
        if (current == '"')
        {
            return ScanQuote(isVerbatim);
        }

        if (!isVerbatim && current == '\n')
        {
            return true;
        }

        if (isInterpolated && current == '{')
        {
            ScanInterpolationHole();
        }
        else
        {
            _position += !isVerbatim && current == '\\' ? PairLength : 1;
        }

        return false;
    }

    private bool ScanQuote(bool isVerbatim)
    {
        if (isVerbatim && Peek(1) == '"')
        {
            _position += PairLength;
            return false;
        }

        _position++;
        return true;
    }

    private void ScanInterpolationHole()
    {
        if (Peek(1) == '{')
        {
            _position += PairLength;
            return;
        }

        _position++;
        ScanCode('}', '(');
    }

    private void ScanMarkupLine()
    {
        while (!AtEnd && Current != '\n')
        {
            if (Current == '@')
            {
                ScanTransition();
            }
            else
            {
                _position++;
            }
        }
    }

    private void ScanMarkupElement()
    {
        var depth = 0;
        while (!AtEnd)
        {
            if (ScanMarkupElementStep() is { } depthChange)
            {
                depth += depthChange;
                if (depth <= 0)
                {
                    return;
                }
            }
        }
    }

    private int? ScanMarkupElementStep()
    {
        if (Current == '@')
        {
            ScanTransition();
            return null;
        }

        if (Current == '<' && Peek(1) == '/')
        {
            SkipPast(">");
            return -1;
        }

        if (Current == '<' && char.IsLetter(Peek(1)))
        {
            return ScanStartTag() ? 1 : 0;
        }

        _position++;
        return null;
    }

    private bool ScanStartTag()
    {
        _position++;
        var name = ReadTagName();
        var isSelfClosing = false;
        while (!AtEnd && Current != '>')
        {
            if (Current == '@')
            {
                ScanTransition();
            }
            else if (Current is '"' or '\'')
            {
                ScanAttributeValue(Current);
            }
            else
            {
                isSelfClosing = Current == '/';
                _position++;
            }
        }

        _position++;
        return !isSelfClosing && !VoidElements.Contains(name);
    }

    private void ScanAttributeValue(char quote)
    {
        _position++;
        while (!AtEnd && Current != quote)
        {
            if (Current == '@')
            {
                ScanTransition();
            }
            else
            {
                _position++;
            }
        }

        _position++;
    }

    private string ReadTagName()
    {
        var start = _position;
        while (!AtEnd && (char.IsLetterOrDigit(Current) || Current is '-' or ':' or '.' or '_'))
        {
            _position++;
        }

        return _text.Substring(start, _position - start);
    }

    private void ScanRazorComment()
    {
        var start = _position;
        _position += PairLength;
        SkipPast("*@");
        _comments.Add(TextSpan.FromBounds(start, _position));
    }

    private string ReadIdentifier()
    {
        var start = _position;
        while (!AtEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
        {
            _position++;
        }

        return _text.Substring(start, _position - start);
    }

    private static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_';

    private void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Current))
        {
            _position++;
        }
    }

    private void SkipToEndOfLine()
    {
        while (!AtEnd && Current != '\n')
        {
            _position++;
        }
    }

    private void SkipPast(string terminator)
    {
        var index = _text.IndexOf(terminator, _position, StringComparison.Ordinal);
        _position = index < 0 ? _text.Length : index + terminator.Length;
    }
}
