using Microsoft.CodeAnalysis.Text;

namespace PklCSharp.Generator.Parser.LexerSubProcessors;

public interface ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, LexerState state);
}
