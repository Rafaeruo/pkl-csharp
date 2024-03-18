using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Parser.LexerStateChangeAction;

namespace PklCSharp.Generator.Parser.LexerSubProcessors;

public class ExactTokenSubProcessor: ILexerSubProcessor
{
    private readonly Dictionary<Tokens, string> _availableExactTokens;

    public ExactTokenSubProcessor()
    {
        _availableExactTokens = typeof(Tokens)
            .GetFields()
            .Where(member => member.GetCustomAttributes(typeof(ExactTokenAttribute), false).Any())
            .ToDictionary(
                member => (Tokens)member.GetRawConstantValue(),
                member => ((ExactTokenAttribute)member.GetCustomAttributes(typeof(ExactTokenAttribute), false).First())
                    .TokenValue);
    }
    
    public IEnumerable<Token> MatchTokens(SourceText source, LexerState state)
    {
        if (state.CurrentMode != LexerMode.Default)
        {
            return Enumerable.Empty<Token>();
        }
        
        var resultsList = new List<Token>();

        foreach (var exactToken in _availableExactTokens)
        {
            var matchResult = MatchExactToken(source, state, exactToken.Value, exactToken.Key);

            if (matchResult != null)
            {
                resultsList.Add(matchResult);
            }
        }

        return resultsList;
    }
    
    private static Token? MatchExactToken(SourceText source, LexerState state, string matchAgainst, Tokens resultType)
    {
        if (state.CurrentPosition + matchAgainst.Length < source.Length && source.GetSubText(new TextSpan(state.CurrentPosition, matchAgainst.Length)).ToString() == matchAgainst)
        {
            return resultType switch
            {
                Tokens.LParen => new ExactToken(
                    resultType,
                    source.Lines.GetLinePosition(state.CurrentPosition),
                    new List<ILexerStateChangeAction>
                    {
                        new AdjustParenLevelAction(1)
                    }),
                Tokens.RParen => ConstructRParenToken(
                    source.Lines.GetLinePosition(state.CurrentPosition),
                    state
                    ),
                _ => new ExactToken(resultType, source.Lines.GetLinePosition(state.CurrentPosition), null)
            };
        }

        return null;
    }

    private static Token ConstructRParenToken(LinePosition position, LexerState state)
    {
        var actions = new List<ILexerStateChangeAction>();
        
        if (state.CurrentParenLevel == 0)
        {
            if (state.ParenLevelScopes.Count > 1)
            {
                actions.Add(new AdjustParenLevelStackAction(true));
                actions.Add(new ModeChangeAction(LexerMode.Pop, position));
            }
        }
        else
        {
            actions.Add(new AdjustParenLevelAction(-1));
        }

        return new ExactToken(
            Tokens.RParen,
            position,
            actions);
    }
}
