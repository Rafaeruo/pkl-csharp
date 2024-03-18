using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Parser.LexerStateChangeAction;

namespace PklCSharp.Generator.Parser;

public record Token(Tokens Type, string? Value, LinePosition? Position, int SourceLength, List<ILexerStateChangeAction> Actions);

public record ExactToken(Tokens Type, LinePosition? Position, List<ILexerStateChangeAction>? Actions)
    : Token(
        Type,
        null,
        Position,
        ((ExactTokenAttribute)typeof(Tokens).GetField(Type.ToString()).GetCustomAttributes(typeof(ExactTokenAttribute), false).First()).TokenValue.Length,
        Actions ?? new List<ILexerStateChangeAction>());
