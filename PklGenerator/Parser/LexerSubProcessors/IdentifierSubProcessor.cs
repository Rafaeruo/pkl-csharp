using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser.LexerSubProcessors;

public class IdentifierSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, int currentPosition)
    {
        var length = 0;
        var discoveredChars = string.Empty;
        
        if (source[currentPosition] == '`')
        {
            while (currentPosition + 1 + length < source.Length &&
                   source[currentPosition + 1 + length] != '`')
            {
                discoveredChars += source[currentPosition + 1 + length];
                length++;
            }

            return new List<Token>
            {
                new Token(Tokens.Identifier, discoveredChars, source.Lines.GetLinePosition(currentPosition), length + 2)
            };
        }

        if (IsIdentifierStart(source[currentPosition]))
        {
            discoveredChars += source[currentPosition];
            length++;

            while (IsIdentifierPart(source[currentPosition + length]))
            {
                discoveredChars += source[currentPosition + length];
                length++;
            }
            
            return new List<Token>
            {
                new Token(Tokens.Identifier, discoveredChars, source.Lines.GetLinePosition(currentPosition), length)
            };
        }

        return Enumerable.Empty<Token>();
    }
    
    private static bool IsIdentifierStart(char c)
    {
        return c == '$' || c == '_' || char.IsLetter(c) || char.GetUnicodeCategory(c) == UnicodeCategory.LetterNumber;
    }
    
    private static bool IsIdentifierPart(char c)
    {
        return IsIdentifierStart(c) || char.IsDigit(c) || char.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark
               || char.GetUnicodeCategory(c) == UnicodeCategory.SpacingCombiningMark || char.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation;
    }
}
