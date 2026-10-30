using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing;

public readonly struct Token
{
    public Token(TokenKind kind, TextSpan span, int newLinesBetween)
    {
        Kind = kind;
        Span = span;
        NewLinesBetween = newLinesBetween;
    }

    public TokenKind Kind { get; }
    public TextSpan Span { get; }

    /// <summary>Number of line breaks between the previous raw token and this one.</summary>
    public int NewLinesBetween { get; }

    public override string ToString() => $"{Kind} {Span}";
}
