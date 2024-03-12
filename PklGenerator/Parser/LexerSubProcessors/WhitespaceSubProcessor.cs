using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser.LexerSubProcessors;

public class WhitespaceSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, int currentPosition)
    {
        var resultsList = new List<Token>();
        
        if (IsWhiteSpaceNotNewLine(source[currentPosition]))
        {
            var length = 0;
            
            while (currentPosition + length < source.Length && IsWhiteSpaceNotNewLine(source[currentPosition + length]))
            {
                length++;
            }

            resultsList.Add(new Token(Tokens.Whitespace, null, source.Lines.GetLinePosition(currentPosition), length));
        }

        if (IsNewLineSemiColonChar(source[currentPosition]))
        {
            var length = 0;
            
            while (currentPosition + length < source.Length && IsNewLineSemiColonChar(source[currentPosition + length]))
            {
                length++;
            }

            resultsList.Add(new Token(Tokens.NewLineSemiColon, null, source.Lines.GetLinePosition(currentPosition), length));
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
