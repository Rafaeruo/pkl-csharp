using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Parser.LexerStateChangeAction;

namespace PklCSharp.Generator.Parser.LexerSubProcessors;

public class CommentSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, LexerState state)
    {
        if (state.CurrentMode != LexerMode.Default)
        {
            return Enumerable.Empty<Token>();
        }
        
        if (TryMatchDocComment(source, state.CurrentPosition, out var docComment, out var docCommentLength))
        {
            return new List<Token> { new Token(Tokens.DocComment, docComment, source.Lines.GetLinePosition(state.CurrentPosition), docCommentLength, new List<ILexerStateChangeAction>()) };
        }
        
        if (TryMatchLineComment(source, state.CurrentPosition, out var lineComment, out var lineCommentLength))
        {
            return new List<Token> { new Token(Tokens.LComment, lineComment, source.Lines.GetLinePosition(state.CurrentPosition), lineCommentLength, new List<ILexerStateChangeAction>()) };
        }
        
        if (TryMatchBlockComment(source, state.CurrentPosition, out var blockComment, out var blockCommentLength))
        {
            return new List<Token> { new Token(Tokens.BComment, blockComment, source.Lines.GetLinePosition(state.CurrentPosition), blockCommentLength, new List<ILexerStateChangeAction>()) };
        }
        
        if (TryMatchShebangComment(source, state.CurrentPosition, out var shebangComment, out var shebangCommentLength))
        {
            return new List<Token> { new Token(Tokens.ShebangComment, shebangComment, source.Lines.GetLinePosition(state.CurrentPosition), shebangCommentLength, new List<ILexerStateChangeAction>()) };
        }
        
        return Enumerable.Empty<Token>();
    }

    private static bool TryMatchDocComment(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (currentPosition + 2 < source.Length && source.GetSubText(new TextSpan(currentPosition, 3)).ToString() == "///")
        {
            var length = 0;
                
            while (currentPosition + 3 + length < source.Length && !IsNewLineChar(source[currentPosition + 3 + length]))
            {
                length++;
            }

            result = source.GetSubText(new TextSpan(currentPosition + 3, length)).ToString();
            shiftCurrentPositionBy = length + 3;
            return true;
        }

        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }
    
    private static bool TryMatchLineComment(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (currentPosition + 1 < source.Length && source.GetSubText(new TextSpan(currentPosition, 2)).ToString() == "//")
        {
            var length = 0;
                
            while (currentPosition + 2 + length < source.Length && !IsNewLineChar(source[currentPosition + 2 + length]))
            {
                length++;
            }

            result = source.GetSubText(new TextSpan(currentPosition + 2, length)).ToString();
            shiftCurrentPositionBy = length + 2;
            return true;
        }

        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }
    
    private static bool TryMatchBlockComment(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (currentPosition + 1 < source.Length && source.GetSubText(new TextSpan(currentPosition, 2)).ToString() == "/*")
        {
            var length = 0;
                
            while (currentPosition + 2 + length < source.Length)
            {
                if (currentPosition + 3 + length < source.Length && source.GetSubText(new TextSpan(currentPosition + 2 + length, 2)).ToString() == "*/")
                {
                    break;
                }
                
                length++;
            }

            result = source.GetSubText(new TextSpan(currentPosition + 2, length)).ToString();
            shiftCurrentPositionBy = length + 4;
            return true;
        }

        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }
    
    private static bool TryMatchShebangComment(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (currentPosition + 1 < source.Length && source.GetSubText(new TextSpan(currentPosition, 2)).ToString() == "#!")
        {
            var length = 0;
                
            while (currentPosition + 2 + length < source.Length && !IsNewLineChar(source[currentPosition + 2 + length]))
            {
                length++;
            }

            result = source.GetSubText(new TextSpan(currentPosition + 2, length)).ToString();
            shiftCurrentPositionBy = length + 2;
            return true;
        }

        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }

    private static bool IsNewLineChar(char c)
    {
        return c is '\n' or '\r';
    }
}
