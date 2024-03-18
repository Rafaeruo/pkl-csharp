using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Parser.LexerStateChangeAction;

namespace PklCSharp.Generator.Parser.LexerSubProcessors;

public class WhitespaceSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, LexerState state)
    {
        if (state.CurrentMode != LexerMode.Default)
        {
            return Enumerable.Empty<Token>();
        }
        
        var resultsList = new List<Token>();
        
        if (IsWhiteSpaceNotNewLine(source[state.CurrentPosition]))
        {
            var length = 0;
            
            while (state.CurrentPosition + length < source.Length && IsWhiteSpaceNotNewLine(source[state.CurrentPosition + length]))
            {
                length++;
            }

            resultsList.Add(new Token(Tokens.Whitespace, null, source.Lines.GetLinePosition(state.CurrentPosition), length, new List<ILexerStateChangeAction>()));
        }

        if (IsNewLineSemiColonChar(source[state.CurrentPosition]))
        {
            var length = 0;
            
            while (state.CurrentPosition + length < source.Length && IsNewLineSemiColonChar(source[state.CurrentPosition + length]))
            {
                length++;
            }

            resultsList.Add(new Token(Tokens.NewLineSemiColon, null, source.Lines.GetLinePosition(state.CurrentPosition), length, new List<ILexerStateChangeAction>()));
        }

        return resultsList;
    }
    
    private static bool IsWhiteSpaceNotNewLine(char c)
    {
        return char.IsWhiteSpace(c) && c != '\n' && c != '\r';
    }
    
    private static bool IsNewLineSemiColonChar(char c)
    {
        return c is '\n' or '\r' or ';';
    }
}
