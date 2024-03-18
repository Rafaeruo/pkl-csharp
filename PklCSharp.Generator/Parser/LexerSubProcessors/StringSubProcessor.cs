using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Parser.LexerStateChangeAction;

namespace PklCSharp.Generator.Parser.LexerSubProcessors;

public class StringSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, LexerState state)
    {
        return state.CurrentMode switch
        {
            LexerMode.Default => DefaultModeMatchTokens(source, state.CurrentPosition),
            LexerMode.SlString => SlModeMatchTokens(source, state.CurrentPosition),
            LexerMode.MlString => MlModeMatchTokens(source, state.CurrentPosition),
            _ => Enumerable.Empty<Token>()
        };
    }

    private IEnumerable<Token> DefaultModeMatchTokens(SourceText source, int currentPosition)
    {
        var poundLength = 0;
        
        if (source[currentPosition] == '#')
        {
            while (source[currentPosition + poundLength] == '#')
            {
                poundLength++;
            }
        }

        if (source[currentPosition + poundLength] == '\"')
        {
            if (source[currentPosition + poundLength + 1] == '\"' && source[currentPosition + poundLength + 2] == '\"')
            {
                return new List<Token>
                {
                    new Token(
                        Tokens.MlString,
                        null,
                        source.Lines.GetLinePosition(currentPosition),
                        3 + poundLength,
                        new List<ILexerStateChangeAction>
                        {
                            new ModeChangeAction(LexerMode.MlString, source.Lines.GetLinePosition(currentPosition))
                        })
                };
            }
            
            return new List<Token>
            {
                new Token(
                    Tokens.SlString,
                    null, source.Lines.GetLinePosition(currentPosition),
                    1 + poundLength,
                    new List<ILexerStateChangeAction>
                    {
                        new ModeChangeAction(LexerMode.SlString, source.Lines.GetLinePosition(currentPosition))
                    })
            };
        }

        return Enumerable.Empty<Token>();
    }
    
    private IEnumerable<Token> SlModeMatchTokens(SourceText source, int currentPosition)
    {
        // TODO: Implement
        return Enumerable.Empty<Token>();
    }
    
    private IEnumerable<Token> MlModeMatchTokens(SourceText source, int currentPosition)
    {
        // TODO: Implement
        return Enumerable.Empty<Token>();
    }
}
