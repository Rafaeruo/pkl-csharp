using PklGenerator.Parsing;

namespace PklGenerator.Tests.Parsing;

public class LexerTests
{
    // Like the reference lexer, string scanning yields empty StringPart tokens before escapes and
    // interpolations. They carry no content, so they are left out here.
    private static List<TokenKind> Kinds(string source) =>
        Lexer.Tokenize(source)
            .Where(t => !(t.Kind == TokenKind.StringPart && t.Span.Length == 0))
            .Select(t => t.Kind)
            .ToList();

    [Fact]
    public void LexesKeywordsAndIdentifiers()
    {
        Assert.Equal(
            new[] { TokenKind.Class, TokenKind.Identifier, TokenKind.Extends, TokenKind.Identifier, TokenKind.Eof },
            Kinds("class Foo extends Bar"));
    }

    [Fact]
    public void LexesImportAndReadVariants()
    {
        Assert.Equal(
            new[] { TokenKind.Import, TokenKind.ImportStar, TokenKind.Read, TokenKind.ReadStar, TokenKind.ReadQuestion, TokenKind.Eof },
            Kinds("import import* read read* read?"));
    }

    [Theory]
    [InlineData("42", TokenKind.Int)]
    [InlineData("1_000", TokenKind.Int)]
    [InlineData("0xFF_FF", TokenKind.Hex)]
    [InlineData("0b1010", TokenKind.Bin)]
    [InlineData("0o777", TokenKind.Oct)]
    [InlineData("1.5", TokenKind.Float)]
    [InlineData(".5", TokenKind.Float)]
    [InlineData("1e10", TokenKind.Float)]
    [InlineData("1.5e-3", TokenKind.Float)]
    public void LexesNumbers(string source, TokenKind expected)
    {
        Assert.Equal(new[] { expected, TokenKind.Eof }, Kinds(source));
    }

    [Fact]
    public void IntFollowedByMemberAccessIsNotAFloat()
    {
        Assert.Equal(
            new[] { TokenKind.Int, TokenKind.Dot, TokenKind.Identifier, TokenKind.Eof },
            Kinds("1.isEven"));
    }

    [Fact]
    public void LexesMultiCharacterOperators()
    {
        Assert.Equal(
            new[]
            {
                TokenKind.Equal, TokenKind.NotEqual, TokenKind.Lte, TokenKind.Gte, TokenKind.And, TokenKind.Or,
                TokenKind.Pipe, TokenKind.Coalesce, TokenKind.QDot, TokenKind.NonNull, TokenKind.Pow,
                TokenKind.IntDiv, TokenKind.Arrow, TokenKind.DColon, TokenKind.Spread, TokenKind.QSpread,
                TokenKind.LPred, TokenKind.Eof,
            },
            Kinds("== != <= >= && || |> ?? ?. !! ** ~/ -> :: ... ...? [["));
    }

    [Fact]
    public void LexesCommentsAsTokens()
    {
        Assert.Equal(
            new[] { TokenKind.LineComment, TokenKind.DocComment, TokenKind.BlockComment, TokenKind.Identifier, TokenKind.Eof },
            Kinds("// line\n/// doc\n/* block */ x"));
    }

    [Fact]
    public void TracksNewlinesBetweenTokens()
    {
        var tokens = Lexer.Tokenize("a\n\n\nb c");
        Assert.Equal(0, tokens[0].NewLinesBetween);
        Assert.Equal(3, tokens[1].NewLinesBetween);
        Assert.Equal(0, tokens[2].NewLinesBetween);
    }

    [Fact]
    public void LexesQuotedIdentifier()
    {
        var tokens = Lexer.Tokenize("`my-name`");
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal(9, tokens[0].Span.Length);
    }

    [Fact]
    public void LexesStringWithInterpolationAndEscapes()
    {
        Assert.Equal(
            new[]
            {
                TokenKind.StringStart, TokenKind.StringPart, TokenKind.InterpolationStart, TokenKind.Identifier,
                TokenKind.RParen, TokenKind.StringEscapeNewline, TokenKind.StringEnd, TokenKind.Eof,
            },
            Kinds("\"a\\(b)\\n\""));
    }

    [Fact]
    public void InterpolationParensDoNotEndTheInterpolationEarly()
    {
        Assert.Equal(
            new[]
            {
                TokenKind.StringStart, TokenKind.InterpolationStart, TokenKind.Identifier, TokenKind.LParen,
                TokenKind.RParen, TokenKind.RParen, TokenKind.StringEnd, TokenKind.Eof,
            },
            Kinds("\"\\(f())\""));
    }

    [Fact]
    public void RawStringsOnlyEscapeWithMatchingPounds()
    {
        Assert.Equal(
            new[] { TokenKind.StringStart, TokenKind.StringPart, TokenKind.StringEnd, TokenKind.Eof },
            Kinds("#\"a\\nb\"#"));
        Assert.Equal(
            new[]
            {
                TokenKind.StringStart, TokenKind.StringPart, TokenKind.StringEscapeNewline, TokenKind.StringPart,
                TokenKind.StringEnd, TokenKind.Eof,
            },
            Kinds("#\"a\\#nb\"#"));
    }

    [Fact]
    public void LexesMultiLineString()
    {
        var kinds = Kinds("\"\"\"\n  hi\n  \"\"\"");
        Assert.Equal(TokenKind.StringMultiStart, kinds[0]);
        Assert.Equal(TokenKind.StringNewline, kinds[1]);
        Assert.Equal(TokenKind.StringEnd, kinds[^2]);
    }

    [Theory]
    [InlineData("&")]
    [InlineData("~")]
    [InlineData("..")]
    [InlineData("^")]
    [InlineData("`unterminated")]
    [InlineData("/* never closed")]
    [InlineData("\"bad \\q escape\"")]
    [InlineData("0x")]
    public void ThrowsOnInvalidInput(string source)
    {
        Assert.Throws<PklParseException>(() => Lexer.Tokenize(source));
    }
}
