using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser.LexerSubProcessors;

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
    
    public IEnumerable<Token> MatchTokens(SourceText source, int currentPosition)
    {
        var resultsList = new List<Token>();

        foreach (var exactToken in _availableExactTokens)
        {
            var matchResult = MatchExactToken(source, currentPosition, exactToken.Value, exactToken.Key);

            if (matchResult != null)
            {
                resultsList.Add(matchResult);
            }
        }

        return resultsList;
    }
    
    private static Token? MatchExactToken(SourceText source, int currentPosition, string matchAgainst, Tokens resultType)
    {
        if (currentPosition + matchAgainst.Length < source.Length && source.GetSubText(new TextSpan(currentPosition, matchAgainst.Length)).ToString() == matchAgainst)
        {
            return new ExactToken(resultType, source.Lines.GetLinePosition(currentPosition));
        }

        return null;
    }
}
