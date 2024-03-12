using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser.LexerSubProcessors;

public class CommentSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, int currentPosition)
    {
        if (TryMatchDocComment(source, currentPosition, out var docComment, out var docCommentLength))
        {
            return new List<Token> { new Token(Tokens.DocComment, docComment, source.Lines.GetLinePosition(currentPosition), docCommentLength) };
        }
        
        if (TryMatchLineComment(source, currentPosition, out var lineComment, out var lineCommentLength))
        {
            return new List<Token> { new Token(Tokens.LComment, lineComment, source.Lines.GetLinePosition(currentPosition), lineCommentLength) };
        }
        
        if (TryMatchBlockComment(source, currentPosition, out var blockComment, out var blockCommentLength))
        {
            return new List<Token> { new Token(Tokens.BComment, blockComment, source.Lines.GetLinePosition(currentPosition), blockCommentLength) };
        }
        
        if (TryMatchShebangComment(source, currentPosition, out var shebangComment, out var shebangCommentLength))
        {
            return new List<Token> { new Token(Tokens.DocComment, shebangComment, source.Lines.GetLinePosition(currentPosition), shebangCommentLength) };
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
