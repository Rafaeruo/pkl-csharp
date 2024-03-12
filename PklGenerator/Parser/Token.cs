using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser;

public record Token(Tokens Type, string? Value, LinePosition? Position, int SourceLength);

public record ExactToken(Tokens Type, LinePosition? Position)
    : Token(Type, null, Position,
        ((ExactTokenAttribute)typeof(Tokens).GetField(Type.ToString()).GetCustomAttributes(typeof(ExactTokenAttribute), false).First()).TokenValue.Length);
