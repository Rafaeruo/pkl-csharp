using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parser.LexerSubProcessors;

public class NumberSubProcessor: ILexerSubProcessor
{
    public IEnumerable<Token> MatchTokens(SourceText source, int currentPosition)
    {
        var results = new List<Token>();

        if (TryParseInt(source, currentPosition, out var intResult, out var intLength))
        {
            results.Add(new Token(Tokens.IntLiteral, intResult, source.Lines.GetLinePosition(currentPosition), intLength));
        }
        
        if (TryParseFloat(source, currentPosition, out var floatResult, out var floatLength))
        {
            results.Add(new Token(Tokens.IntLiteral, intResult, source.Lines.GetLinePosition(currentPosition), floatLength));
        }
        
        return results;
    }
    
    private bool TryParseInt(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (currentPosition + 2 < source.Length)
        {
            if (source.GetSubText(new TextSpan(currentPosition, 2)).ToString() == "0x"
                && (IsBetween(source[currentPosition + 2], '0', '9')
                    || IsBetween(source[currentPosition + 2], 'a', 'f')
                    || IsBetween(source[currentPosition + 2], 'A', 'F')
                    || source[currentPosition + 2] == '_'))
            {
                var length = 0;
                var number = "0x";

                while (currentPosition + 2 + length < source.Length
                       && (IsBetween(source[currentPosition + 2 + length], '0', '9')
                           || IsBetween(source[currentPosition + 2 + length], 'a', 'f')
                           || IsBetween(source[currentPosition + 2 + length], 'A', 'F')
                           || source[currentPosition + 2 + length] == '_'))
                {
                    number += source[currentPosition + 2 + length];
                    length++;
                }

                result = number;
                shiftCurrentPositionBy = length + 2;
                return true;
            }
            
            if (source.GetSubText(new TextSpan(currentPosition, 2)).ToString() == "0o"
                && (IsBetween(source[currentPosition + 2], '0', '7') || source[currentPosition + 2] == '_'))
            {
                var length = 0;
                var number = "0o";

                while (currentPosition + 2 + length < source.Length && IsBetween(source[currentPosition + 2 + length], '0', '7') || source[currentPosition + 2 + length] == '_')
                {
                    number += source[currentPosition + 2 + length];
                    length++;
                }

                result = number;
                shiftCurrentPositionBy = length + 2;
                return true;
            }
            
            if (source.GetSubText(new TextSpan(currentPosition, 2)).ToString() == "0b"
                && (IsBetween(source[currentPosition + 2], '0', '1') || source[currentPosition + 2] == '_'))
            {
                var length = 0;
                var number = "0b";

                while (currentPosition + 2 + length < source.Length && IsBetween(source[currentPosition + 2 + length], '0', '1') || source[currentPosition + 2 + length] == '_')
                {
                    number += source[currentPosition + 2 + length];
                    length++;
                }

                result = number;
                shiftCurrentPositionBy = length + 2;
                return true;
            }
        }

        if (TryParseDecimals(source, currentPosition, out var decimalNumber, out var decimalLength))
        {
            result = decimalNumber;
            shiftCurrentPositionBy = decimalLength;
            return true;
        }
        
        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }

    private bool TryParseFloat(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (TryParseDecimals(source, currentPosition, out var part1, out var part1Length))
        {
            if (TryParseExponent(source, currentPosition + part1Length, out var part2, out var part2Length))
            {
                result = part1 + part2;
                shiftCurrentPositionBy = part1Length + part2Length;
                return true;
            }
        }

        if (source[currentPosition + part1Length] == '.')
        {
            result = part1 + '.';
            shiftCurrentPositionBy = part1Length + 1;

            if (source[currentPosition + shiftCurrentPositionBy] == '_')
            {
                result += '_';
                shiftCurrentPositionBy++;
            }
            
            if (TryParseDecimals(source, currentPosition + shiftCurrentPositionBy, out var part2, out var part2Length))
            {
                TryParseExponent(source, currentPosition + shiftCurrentPositionBy + part2Length, out var part3,
                    out var part3Length);
                
                result += part2 + part3;
                shiftCurrentPositionBy += part2Length + part3Length;
                return true;
            }
        }
        
        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }

    private bool TryParseDecimals(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (IsBetween(source[currentPosition], '0', '9'))
        {
            var length = 0;
            var number = string.Empty;

            while (currentPosition + length < source.Length && IsBetween(source[currentPosition + length], '0', '9') || source[currentPosition + length] == '_')
            {
                number += source[currentPosition + length];
                length++;
            }

            result = number;
            shiftCurrentPositionBy = length;
            return true;
        }
        
        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }

    private bool TryParseExponent(SourceText source, int currentPosition, out string result, out int shiftCurrentPositionBy)
    {
        if (source[currentPosition] == 'e' || source[currentPosition] == 'E')
        {
            var exponentString = source[currentPosition];
            var shift = 1;

            if (source[currentPosition + shift] == '+' || source[currentPosition + shift] == '-')
            {
                exponentString += source[currentPosition + shift];
                shift++;
            }
            
            if (source[currentPosition + shift] == '_')
            {
                exponentString += source[currentPosition + shift];
                shift++;
            }

            if (TryParseDecimals(source, currentPosition, out var decimalValue, out var decimalLength))
            {
                result = exponentString + decimalValue;
                shiftCurrentPositionBy = shift + decimalLength;
                return true;
            }
        }
        
        result = string.Empty;
        shiftCurrentPositionBy = 0;
        return false;
    }
    
    private static bool IsBetween(char c, char minInclusive, char maxInclusive) =>
        (uint)(c - minInclusive) <= (uint)(maxInclusive - minInclusive);
}
