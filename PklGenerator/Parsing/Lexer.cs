using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing;

/// <summary>
/// Converts Pkl source text into tokens. This is a port of the lexer in the Pkl reference
/// implementation (pkl-parser's Lexer.java), including its string/interpolation state machine.
/// Comments and semicolons are returned as tokens; the parser decides to skip them.
/// </summary>
public sealed class Lexer
{
    private const int Eof = -1;

    private enum State
    {
        Default,
        String,
    }

    private sealed class InterpolationScope
    {
        public InterpolationScope(int quotes, int pounds)
        {
            Quotes = quotes;
            Pounds = pounds;
        }

        public int Quotes { get; }
        public int Pounds { get; }
        public int Parens { get; set; }
    }

    private readonly string _source;
    private readonly int _size;
    private readonly Stack<InterpolationScope> _interpolationStack = new();
    private int _cursor;
    private int _startCursor;
    private int _lookahead;
    private State _state = State.Default;
    private bool _stringEnded;
    private bool _isEscape;
    private int _newLinesBetween;

    public Lexer(string source)
    {
        _source = source;
        _size = source.Length;
        _lookahead = _size > 0 ? source[0] : Eof;
    }

    public static List<Token> Tokenize(string source)
    {
        var lexer = new Lexer(source);
        var tokens = new List<Token>();
        while (true)
        {
            var kind = lexer.Next();
            tokens.Add(new Token(kind, lexer.Span, lexer._newLinesBetween));
            if (kind == TokenKind.Eof)
            {
                return tokens;
            }
        }
    }

    /// <summary>The span of the last lexed token.</summary>
    public TextSpan Span => new(_startCursor, _cursor - _startCursor);

    public TokenKind Next()
    {
        _startCursor = _cursor;
        _newLinesBetween = 0;
        return _state == State.Default ? NextDefault() : NextString();
    }

    private TokenKind NextDefault()
    {
        var ch = NextChar();
        // ignore whitespace
        while (ch is ' ' or '\n' or '\t' or '\f' or '\r')
        {
            _startCursor = _cursor;
            if (ch == '\n')
            {
                _newLinesBetween++;
            }

            ch = NextChar();
        }

        switch (ch)
        {
            case Eof:
                // when EOF is reached we overshot the span
                _cursor--;
                return TokenKind.Eof;
            case ';':
                return TokenKind.Semicolon;
            case '(':
                if (_interpolationStack.Count > 0)
                {
                    _interpolationStack.Peek().Parens++;
                }

                return TokenKind.LParen;
            case ')':
                if (_interpolationStack.Count > 0)
                {
                    var scope = _interpolationStack.Peek();
                    scope.Parens--;
                    if (scope.Parens <= 0)
                    {
                        // interpolation is over, back to the string
                        _state = State.String;
                    }
                }

                return TokenKind.RParen;
            case '{':
                return TokenKind.LBrace;
            case '}':
                return TokenKind.RBrace;
            case ',':
                return TokenKind.Comma;
            case '@':
                return TokenKind.At;
            case ':':
                return Accept(':') ? TokenKind.DColon : TokenKind.Colon;
            case '+':
                return TokenKind.Plus;
            case '%':
                return TokenKind.Mod;
            case '[':
                return Accept('[') ? TokenKind.LPred : TokenKind.LBrack;
            case ']':
                return TokenKind.RBrack;
            case '=':
                return Accept('=') ? TokenKind.Equal : TokenKind.Assign;
            case '>':
                return Accept('=') ? TokenKind.Gte : TokenKind.Gt;
            case '<':
                return Accept('=') ? TokenKind.Lte : TokenKind.Lt;
            case '-':
                return Accept('>') ? TokenKind.Arrow : TokenKind.Minus;
            case '!':
                if (Accept('!'))
                {
                    return TokenKind.NonNull;
                }

                return Accept('=') ? TokenKind.NotEqual : TokenKind.Not;
            case '?':
                if (Accept('.'))
                {
                    return TokenKind.QDot;
                }

                return Accept('?') ? TokenKind.Coalesce : TokenKind.Question;
            case '&':
                if (Accept('&'))
                {
                    return TokenKind.And;
                }

                throw UnexpectedChar(ch, "&&");
            case '|':
                if (Accept('>'))
                {
                    return TokenKind.Pipe;
                }

                return Accept('|') ? TokenKind.Or : TokenKind.Union;
            case '*':
                return Accept('*') ? TokenKind.Pow : TokenKind.Star;
            case '~':
                if (Accept('/'))
                {
                    return TokenKind.IntDiv;
                }

                throw UnexpectedChar(ch, "~/");
            case '.':
                if (_lookahead == '.')
                {
                    NextChar();
                    if (_lookahead == '.')
                    {
                        NextChar();
                        return Accept('?') ? TokenKind.QSpread : TokenKind.Spread;
                    }

                    throw LexError("Unexpected token `..`, expected `.`, `...` or `...?`", ErrorSpan());
                }

                if (_lookahead >= '0' && _lookahead <= '9')
                {
                    return LexNumber(ch);
                }

                return TokenKind.Dot;
            case '`':
                LexQuotedIdentifier();
                return TokenKind.Identifier;
            case '/':
                return LexSlash();
            case '"':
                return LexStringStart(0);
            case '#':
                return _lookahead == '!' ? LexShebang() : LexStringStartPounds();
            default:
                if (IsAsciiDigit(ch))
                {
                    return LexNumber(ch);
                }

                if (IsIdentifierStart(ch))
                {
                    return LexIdentifier();
                }

                throw LexError($"Invalid character `{(char)ch}`", new TextSpan(_cursor - 1, 1));
        }
    }

    private TokenKind NextString()
    {
        var scope = _interpolationStack.Peek();
        if (_stringEnded)
        {
            LexStringEnd(scope);
            _stringEnded = false;
            _interpolationStack.Pop();
            _state = State.Default;
            return TokenKind.StringEnd;
        }

        if (_lookahead == Eof)
        {
            return TokenKind.Eof;
        }

        if (_isEscape)
        {
            _isEscape = false;
            // consume the `\#*`
            for (var i = 0; i < scope.Pounds + 1; i++)
            {
                NextChar();
            }

            return LexEscape();
        }

        if (scope.Quotes == 1)
        {
            LexString(scope.Pounds);
        }
        else
        {
            if (_lookahead == '\r')
            {
                NextChar();
                if (_lookahead == '\n')
                {
                    NextChar();
                }

                return TokenKind.StringNewline;
            }

            if (_lookahead == '\n')
            {
                NextChar();
                return TokenKind.StringNewline;
            }

            LexMultiString(scope.Pounds);
        }

        return TokenKind.StringPart;
    }

    private TokenKind LexStringStartPounds()
    {
        var pounds = 1;
        while (_lookahead == '#')
        {
            NextChar();
            pounds++;
        }

        if (_lookahead == Eof)
        {
            throw LexError("Unexpected end of file", Span);
        }

        if (_lookahead != '"')
        {
            throw UnexpectedChar(_lookahead, "\"");
        }

        NextChar();
        return LexStringStart(pounds);
    }

    private TokenKind LexStringStart(int pounds)
    {
        var quotes = 1;
        if (_lookahead == '"')
        {
            NextChar();
            if (_lookahead == '"')
            {
                NextChar();
                quotes = 3;
            }
            else
            {
                Backup();
            }
        }

        _state = State.String;
        _interpolationStack.Push(new InterpolationScope(quotes, pounds));
        _stringEnded = false;
        return quotes == 1 ? TokenKind.StringStart : TokenKind.StringMultiStart;
    }

    private void LexStringEnd(InterpolationScope scope)
    {
        for (var i = 0; i < scope.Quotes + scope.Pounds; i++)
        {
            NextChar();
        }
    }

    private void LexString(int pounds)
    {
        var poundsInARow = 0;
        var foundQuote = false;
        var foundBackslash = false;
        while (_lookahead != Eof)
        {
            var ch = NextChar();
            switch (ch)
            {
                case '\n':
                case '\r':
                    throw LexError($"Missing closing delimiter `\"{new string('#', pounds)}`", new TextSpan(_cursor - 1, 1));
                case '"':
                    if (pounds == 0)
                    {
                        Backup();
                        _stringEnded = true;
                        return;
                    }

                    foundQuote = true;
                    foundBackslash = false;
                    poundsInARow = 0;
                    break;
                case '\\':
                    foundQuote = false;
                    foundBackslash = true;
                    poundsInARow = 0;
                    if (pounds == poundsInARow)
                    {
                        Backup(pounds + 1);
                        _isEscape = true;
                        return;
                    }

                    break;
                case '#':
                    poundsInARow++;
                    if (foundQuote && pounds == poundsInARow)
                    {
                        Backup(pounds + 1);
                        _stringEnded = true;
                        return;
                    }

                    if (foundBackslash && pounds == poundsInARow)
                    {
                        Backup(pounds + 1);
                        _isEscape = true;
                        return;
                    }

                    break;
                default:
                    foundQuote = false;
                    foundBackslash = false;
                    poundsInARow = 0;
                    break;
            }
        }
    }

    private void LexMultiString(int pounds)
    {
        var poundsInARow = 0;
        var quotesInARow = 0;
        var foundBackslash = false;
        while (_lookahead != Eof && _lookahead != '\n' && _lookahead != '\r')
        {
            var ch = NextChar();
            switch (ch)
            {
                case '"':
                    quotesInARow++;
                    if (quotesInARow == 3 && pounds == 0)
                    {
                        Backup(3);
                        _stringEnded = true;
                        return;
                    }

                    poundsInARow = 0;
                    foundBackslash = false;
                    break;
                case '\\':
                    quotesInARow = 0;
                    poundsInARow = 0;
                    foundBackslash = true;
                    if (pounds == poundsInARow)
                    {
                        Backup(pounds + 1);
                        _isEscape = true;
                        return;
                    }

                    break;
                case '#':
                    poundsInARow++;
                    if (quotesInARow == 3 && pounds == poundsInARow)
                    {
                        Backup(pounds + 3);
                        _stringEnded = true;
                        return;
                    }

                    if (foundBackslash && pounds == poundsInARow)
                    {
                        Backup(pounds + 1);
                        _isEscape = true;
                        return;
                    }

                    break;
                default:
                    quotesInARow = 0;
                    poundsInARow = 0;
                    foundBackslash = false;
                    break;
            }
        }
    }

    private TokenKind LexEscape()
    {
        if (_lookahead == Eof)
        {
            throw UnexpectedEndOfFile();
        }

        var ch = NextChar();
        switch (ch)
        {
            case 'n':
                return TokenKind.StringEscapeNewline;
            case '"':
                return TokenKind.StringEscapeQuote;
            case '\\':
                return TokenKind.StringEscapeBackslash;
            case 't':
                return TokenKind.StringEscapeTab;
            case 'r':
                return TokenKind.StringEscapeReturn;
            case '(':
                _interpolationStack.Peek().Parens++;
                _state = State.Default;
                return TokenKind.InterpolationStart;
            case 'u':
                return LexUnicodeEscape();
            case '\n':
                return TokenKind.StringEscapeContinuation;
            case '\r':
                if (_lookahead == '\n')
                {
                    NextChar();
                }

                return TokenKind.StringEscapeContinuation;
            case ' ':
            case '\t':
                var start = _cursor;
                var next = NextChar();
                while (next is ' ' or '\t')
                {
                    next = NextChar();
                }

                if (next is '\n' or '\r')
                {
                    throw LexError(
                        "Line continuation escape sequences must be directly followed by a newline",
                        new TextSpan(start - 2, _cursor - start + 2));
                }

                throw LexError($"Invalid escape sequence `\\{(char)ch}`", new TextSpan(start - 2, 2));
            default:
                throw LexError($"Invalid escape sequence `\\{(char)ch}`", new TextSpan(_cursor - 2, 2));
        }
    }

    private TokenKind LexUnicodeEscape()
    {
        if (_lookahead != '{')
        {
            throw UnexpectedChar(_lookahead, "{");
        }

        do
        {
            NextChar();
        } while (_lookahead != '}' && _lookahead != Eof && char.IsLetterOrDigit((char)_lookahead));

        if (_lookahead == '}')
        {
            // consume the closing brace
            NextChar();
        }
        else
        {
            throw LexError("Unterminated unicode escape sequence", Span);
        }

        return TokenKind.StringEscapeUnicode;
    }

    private TokenKind LexIdentifier()
    {
        while (IsIdentifierPart(_lookahead))
        {
            NextChar();
        }

        var kind = GetKeywordOrIdentifier(_source.Substring(_startCursor, _cursor - _startCursor));
        switch (kind)
        {
            case TokenKind.Import:
                return Accept('*') ? TokenKind.ImportStar : TokenKind.Import;
            case TokenKind.Read:
                if (Accept('*'))
                {
                    return TokenKind.ReadStar;
                }

                return Accept('?') ? TokenKind.ReadQuestion : TokenKind.Read;
            default:
                return kind;
        }
    }

    private void LexQuotedIdentifier()
    {
        while (_lookahead != '`' && _lookahead != '\n' && _lookahead != '\r' && _lookahead != Eof)
        {
            NextChar();
        }

        if (_lookahead == '`')
        {
            NextChar();
        }
        else
        {
            throw UnexpectedChar(_lookahead, "backquote");
        }
    }

    private TokenKind LexNumber(int start)
    {
        if (start == '0')
        {
            if (_lookahead is 'x' or 'X')
            {
                NextChar();
                LexHexNumber();
                return TokenKind.Hex;
            }

            if (_lookahead is 'b' or 'B')
            {
                NextChar();
                LexBinNumber();
                return TokenKind.Bin;
            }

            if (_lookahead is 'o' or 'O')
            {
                NextChar();
                LexOctNumber();
                return TokenKind.Oct;
            }

            if (_lookahead is 'e' or 'E')
            {
                NextChar();
                LexExponent();
                return TokenKind.Float;
            }
        }
        else if (start == '.')
        {
            LexDotNumber();
            return TokenKind.Float;
        }

        while ((_lookahead >= '0' && _lookahead <= '9') || _lookahead == '_')
        {
            NextChar();
        }

        if (_lookahead is 'e' or 'E')
        {
            NextChar();
            LexExponent();
            return TokenKind.Float;
        }

        if (_lookahead == '.')
        {
            NextChar();
            if (_lookahead == '_')
            {
                throw InvalidSeparatorPosition();
            }

            if (_lookahead < '0' || _lookahead > '9')
            {
                // `1.foo` is a member access on an Int
                Backup();
                return TokenKind.Int;
            }

            LexDotNumber();
            return TokenKind.Float;
        }

        return TokenKind.Int;
    }

    private TokenKind LexSlash()
    {
        switch (_lookahead)
        {
            case '/':
                NextChar();
                var kind = _lookahead == '/' ? TokenKind.DocComment : TokenKind.LineComment;
                while (_lookahead != '\n' && _lookahead != '\r' && _lookahead != Eof)
                {
                    NextChar();
                }

                return kind;
            case '*':
                NextChar();
                LexBlockComment();
                return TokenKind.BlockComment;
            default:
                return TokenKind.Div;
        }
    }

    private void LexBlockComment()
    {
        if (_lookahead == Eof)
        {
            throw UnexpectedEndOfFile();
        }

        var prev = NextChar();
        while (_lookahead != Eof)
        {
            if (prev == '*' && _lookahead == '/')
            {
                NextChar();
                return;
            }

            prev = NextChar();
        }

        throw UnexpectedEndOfFile();
    }

    private void LexHexNumber()
    {
        if (_lookahead == '_')
        {
            throw InvalidSeparatorPosition();
        }

        if (!IsHex(_lookahead))
        {
            throw UnexpectedChar(_lookahead, "hexadecimal number");
        }

        while (IsHex(_lookahead) || _lookahead == '_')
        {
            NextChar();
        }
    }

    private void LexBinNumber()
    {
        if (_lookahead == '_')
        {
            throw InvalidSeparatorPosition();
        }

        if (_lookahead is not ('0' or '1'))
        {
            throw UnexpectedChar(_lookahead, "binary number");
        }

        while (_lookahead is '0' or '1' or '_')
        {
            NextChar();
        }
    }

    private void LexOctNumber()
    {
        if (_lookahead == '_')
        {
            throw InvalidSeparatorPosition();
        }

        if (!(_lookahead >= '0' && _lookahead <= '7'))
        {
            throw UnexpectedChar(_lookahead, "octal number");
        }

        while ((_lookahead >= '0' && _lookahead <= '7') || _lookahead == '_')
        {
            NextChar();
        }
    }

    private void LexExponent()
    {
        if (_lookahead is '+' or '-')
        {
            NextChar();
        }

        if (_lookahead == '_')
        {
            throw InvalidSeparatorPosition();
        }

        if (_lookahead < '0' || _lookahead > '9')
        {
            throw UnexpectedChar(_lookahead, "number");
        }

        while ((_lookahead >= '0' && _lookahead <= '9') || _lookahead == '_')
        {
            NextChar();
        }
    }

    private void LexDotNumber()
    {
        if (_lookahead == '_')
        {
            throw InvalidSeparatorPosition();
        }

        while ((_lookahead >= '0' && _lookahead <= '9') || _lookahead == '_')
        {
            NextChar();
        }

        if (_lookahead is 'e' or 'E')
        {
            NextChar();
            LexExponent();
        }
    }

    private TokenKind LexShebang()
    {
        do
        {
            NextChar();
        } while (_lookahead != '\n' && _lookahead != '\r' && _lookahead != Eof);

        return TokenKind.Shebang;
    }

    private static bool IsAsciiDigit(int c) => c >= '0' && c <= '9';

    private static bool IsHex(int c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static bool IsIdentifierStart(int c)
    {
        if (c == '_' || c == '$')
        {
            return true;
        }

        if (c == Eof)
        {
            return false;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory((char)c);
        return category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber;
    }

    private static bool IsIdentifierPart(int c)
    {
        if (c == Eof)
        {
            return false;
        }

        if (c == '$' || IsIdentifierStart(c))
        {
            return true;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory((char)c);
        return category is UnicodeCategory.DecimalDigitNumber or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.Format;
    }

    private bool Accept(char expected)
    {
        if (_lookahead != expected)
        {
            return false;
        }

        NextChar();
        return true;
    }

    private int NextChar()
    {
        var current = _lookahead;
        _cursor++;
        _lookahead = _cursor >= _size ? Eof : _source[_cursor];
        return current;
    }

    private void Backup()
    {
        _lookahead = _source[--_cursor];
    }

    private void Backup(int amount)
    {
        _cursor -= amount;
        _lookahead = _source[_cursor];
    }

    private TextSpan ErrorSpan()
    {
        var length = _lookahead == Eof ? 0 : 1;
        var index = _lookahead == Eof ? _cursor - 1 : _cursor;
        return new TextSpan(Math.Max(index, 0), length);
    }

    private static PklParseException LexError(string message, TextSpan span) => new(message, span);

    private PklParseException InvalidSeparatorPosition() =>
        LexError("Invalid position for the digit separator `_`", ErrorSpan());

    private PklParseException UnexpectedChar(int got, string expected)
    {
        var gotText = got == Eof ? "EOF" : ((char)got).ToString();
        return LexError($"Unexpected character `{gotText}`, expected `{expected}`", ErrorSpan());
    }

    private PklParseException UnexpectedEndOfFile() =>
        LexError("Unexpected end of file", new TextSpan(_cursor, 0));

    private static TokenKind GetKeywordOrIdentifier(string text) => text switch
    {
        "_" => TokenKind.Underscore,
        "abstract" => TokenKind.Abstract,
        "amends" => TokenKind.Amends,
        "as" => TokenKind.As,
        "case" => TokenKind.Case,
        "class" => TokenKind.Class,
        "const" => TokenKind.Const,
        "delete" => TokenKind.Delete,
        "else" => TokenKind.Else,
        "extends" => TokenKind.Extends,
        "external" => TokenKind.External,
        "false" => TokenKind.False,
        "fixed" => TokenKind.Fixed,
        "for" => TokenKind.For,
        "function" => TokenKind.Function,
        "hidden" => TokenKind.Hidden,
        "if" => TokenKind.If,
        "import" => TokenKind.Import,
        "in" => TokenKind.In,
        "is" => TokenKind.Is,
        "let" => TokenKind.Let,
        "local" => TokenKind.Local,
        "module" => TokenKind.Module,
        "new" => TokenKind.New,
        "nothing" => TokenKind.Nothing,
        "null" => TokenKind.Null,
        "open" => TokenKind.Open,
        "out" => TokenKind.Out,
        "outer" => TokenKind.Outer,
        "override" => TokenKind.Override,
        "protected" => TokenKind.Protected,
        "read" => TokenKind.Read,
        "record" => TokenKind.Record,
        "super" => TokenKind.Super,
        "switch" => TokenKind.Switch,
        "this" => TokenKind.This,
        "throw" => TokenKind.Throw,
        "trace" => TokenKind.Trace,
        "true" => TokenKind.True,
        "typealias" => TokenKind.TypeAlias,
        "unknown" => TokenKind.Unknown,
        "vararg" => TokenKind.Vararg,
        "when" => TokenKind.When,
        _ => TokenKind.Identifier,
    };
}
