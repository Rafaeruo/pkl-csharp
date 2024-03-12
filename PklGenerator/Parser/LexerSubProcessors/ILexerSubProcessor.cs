using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser.LexerSubProcessors;

public interface ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, int currentPosition);
}
