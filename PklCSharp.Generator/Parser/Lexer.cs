using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Exceptions;
using PklCSharp.Generator.Parser.LexerStateChangeAction;
using PklCSharp.Generator.Parser.LexerSubProcessors;

namespace PklCSharp.Generator.Parser;

public class Lexer
{
    private readonly SourceText _source;
    private readonly LexerState _state;
    private readonly List<ILexerSubProcessor> _subProcessors;

    public Lexer(SourceText source)
    {
        _source = source;
        _state = new LexerState();
        
        _subProcessors = new List<ILexerSubProcessor>
        {
            new CommentSubProcessor(),
            new ExactTokenSubProcessor(),
            new IdentifierSubProcessor(),
            new NumberSubProcessor(),
            new StringSubProcessor(),
            new WhitespaceSubProcessor()
        };
    }

    public Token NextToken()
    {
        if (_state.CurrentPosition >= _source.Length)
        {
            return new Token(Tokens.EOF, null, null, 0, new List<ILexerStateChangeAction>());
        }

        var allPossibleTokens = MatchAllPossibleTokens(_source, _state);

        if (allPossibleTokens.Count == 0)
        {
            // TODO: For now, we are reusing the unknown token to debug our code
            _state.CurrentPosition++;
            return new Token(Tokens.Unknown, $"<EXCEPTION>{_source[_state.CurrentPosition - 1]}</EXCEPTION>", _source.Lines.GetLinePosition(_state.CurrentPosition - 1), 1, new List<ILexerStateChangeAction>());
            throw new LexerException("Met an unknown token", _source.Lines.GetLinePosition(_state.CurrentPosition));
        }
        
        // The most expressive token (the one represented by the longest source string) is the token that we return
        // For example, if we had this in our source code:
        // outer throwback
        // we'd want the tokens "outer" and Identifier("throwback"), not "out" and "throw" which are shorter
        var mostExpressiveToken = allPossibleTokens.OrderByDescending(token => token.SourceLength).First();

        CheckParenScopes(mostExpressiveToken.Type);
        
        foreach (var stateChangeAction in mostExpressiveToken.Actions)
        {
            stateChangeAction.Execute(_state);
        }
        
        _state.CurrentPosition += mostExpressiveToken.SourceLength;
        
        return mostExpressiveToken;
    }

    private List<Token> MatchAllPossibleTokens(SourceText source, LexerState state)
    {
        var results = new List<Token>();

        foreach (var subProcessor in _subProcessors)
        {
            results.AddRange(subProcessor.MatchTokens(source, state));
        }

        return results;
    }

    private void CheckParenScopes(Tokens currentTokenType)
    {
        if (currentTokenType == Tokens.StringInterpolation)
        {
            _state.ParenLevelScopes.Push(0);
        }
    }
}
