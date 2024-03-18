using System.Globalization;
using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Parser.LexerStateChangeAction;

namespace PklCSharp.Generator.Parser.LexerSubProcessors;

public class IdentifierSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, LexerState state)
    {
        if (state.CurrentMode != LexerMode.Default)
        {
            return Enumerable.Empty<Token>();
        }
        
        var length = 0;
        var discoveredChars = string.Empty;
        
        if (source[state.CurrentPosition] == '`')
        {
            while (state.CurrentPosition + 1 + length < source.Length &&
                   source[state.CurrentPosition + 1 + length] != '`')
            {
                discoveredChars += source[state.CurrentPosition + 1 + length];
                length++;
            }

            return new List<Token>
            {
                new Token(Tokens.Identifier, discoveredChars, source.Lines.GetLinePosition(state.CurrentPosition), length + 2, new List<ILexerStateChangeAction>())
            };
        }

        if (IsIdentifierStart(source[state.CurrentPosition]))
        {
            discoveredChars += source[state.CurrentPosition];
            length++;

            while (IsIdentifierPart(source[state.CurrentPosition + length]))
            {
                discoveredChars += source[state.CurrentPosition + length];
                length++;
            }
            
            return new List<Token>
            {
                new Token(Tokens.Identifier, discoveredChars, source.Lines.GetLinePosition(state.CurrentPosition), length, new List<ILexerStateChangeAction>())
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
