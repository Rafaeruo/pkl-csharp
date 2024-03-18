using Microsoft.CodeAnalysis.Text;

namespace PklCSharp.Generator.Exceptions;

public class LexerException: Exception
{
    public LinePosition Position { get; init; }

    public LexerException(string issue, LinePosition position) : base($"Lexer Error: {issue} at {position.ToString()}")
    {
        Position = position;
    }
}
