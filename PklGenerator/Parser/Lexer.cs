using Microsoft.CodeAnalysis.Text;
using PklGenerator.Exceptions;
using PklGenerator.Parser.LexerSubProcessors;

namespace PklGenerator.Parser;

public class Lexer
{
    private readonly SourceText _source;
    private int _tokenStartPosition;
    private readonly List<ILexerSubProcessor> _subProcessors;

    public Lexer(SourceText source)
    {
        _source = source;
        _tokenStartPosition = 0;
        _subProcessors = new List<ILexerSubProcessor>
        {
            new CommentSubProcessor(),
            new ExactTokenSubProcessor(),
            new IdentifierSubProcessor(),
            new NumberSubProcessor(),
            new WhitespaceSubProcessor()
        };
    }

    public Token NextToken()
    {
        if (_tokenStartPosition >= _source.Length)
        {
            return new Token(Tokens.EOF, null, null, 0);
        }

        var allPossibleTokens = MatchAllPossibleTokens(_source, _tokenStartPosition);

        if (allPossibleTokens.Count == 0)
        {
            // TODO: For now, we are reusing the unknown token to debug our code
            _tokenStartPosition++;
            return new Token(Tokens.Unknown, $"<EXCEPTION>{_source[_tokenStartPosition - 1]}</EXCEPTION>", _source.Lines.GetLinePosition(_tokenStartPosition - 1), 1);
            throw new LexerException("Met an unknown token", _source.Lines.GetLinePosition(_tokenStartPosition));
        }
        
        // The most expressive token (the one represented by the longest source string) is the token that we return
        // For example, if we had this in our source code:
        // outer throwback
        // we'd want the tokens "outer" and Identifier("throwback"), not "out" and "throw" which are shorter
        var mostExpressiveToken = allPossibleTokens.OrderByDescending(token => token.SourceLength).First();
        
        _tokenStartPosition += mostExpressiveToken.SourceLength;
        return mostExpressiveToken;
    }

    private List<Token> MatchAllPossibleTokens(SourceText source, int currentPosition)
    {
        var results = new List<Token>();

        foreach (var subProcessor in _subProcessors)
        {
            results.AddRange(subProcessor.MatchTokens(source, currentPosition));
        }

        return results;
    }
}
