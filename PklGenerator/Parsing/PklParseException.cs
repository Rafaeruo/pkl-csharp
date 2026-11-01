using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing;

public class PklParseException : Exception
{
    public PklParseException(string message, TextSpan span) : base(message)
    {
        Span = span;
    }

    public TextSpan Span { get; }
}
