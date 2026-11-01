using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Generation;

/// <summary>Converts Pkl literal expressions into C# initializers and infers types from them.</summary>
internal static class Literals
{
    private static readonly Dictionary<string, (decimal Min, decimal Max)> IntegerRanges = new()
    {
        ["sbyte"] = (sbyte.MinValue, sbyte.MaxValue),
        ["byte"] = (byte.MinValue, byte.MaxValue),
        ["short"] = (short.MinValue, short.MaxValue),
        ["ushort"] = (ushort.MinValue, ushort.MaxValue),
        ["int"] = (int.MinValue, int.MaxValue),
        ["uint"] = (uint.MinValue, uint.MaxValue),
        ["long"] = (long.MinValue, long.MaxValue),
        ["ulong"] = (ulong.MinValue, ulong.MaxValue),
    };

    /// <summary>The type of a literal, used for properties declared without a type annotation.</summary>
    public static CSharpType? Infer(Expr expr)
    {
        switch (expr)
        {
            case StringLiteralExpr:
                return CSharpType.String;
            case BoolLiteralExpr:
                return new CSharpType("bool", true);
            case IntLiteralExpr:
                return new CSharpType("long", true);
            case FloatLiteralExpr:
                return new CSharpType("double", true);
            case UnaryMinusExpr { Operand: IntLiteralExpr or FloatLiteralExpr } negated:
                return Infer(negated.Operand);
            case ParenthesizedExpr parenthesized:
                return Infer(parenthesized.Inner);
            default:
                return null;
        }
    }

    /// <summary>
    /// Returns a C# initializer for the expression if it is a literal that fits the target type,
    /// otherwise null. Anything that isn't a plain literal is left to the Pkl evaluator.
    /// </summary>
    public static string? TryFormat(Expr expr, CSharpType target)
    {
        switch (expr)
        {
            case ParenthesizedExpr parenthesized:
                return TryFormat(parenthesized.Inner, target);
            case StringLiteralExpr str when target.Name == "string" && str.ConstantValue is { } value:
                return SymbolDisplay.FormatLiteral(value, quote: true);
            case StringLiteralExpr character when target.Name == "char"
                && character.ConstantValue is { Length: 1 } text
                && !char.IsSurrogate(text[0]):
                return SymbolDisplay.FormatLiteral(text[0], quote: true);
            case BoolLiteralExpr boolean when target.Name == "bool":
                return boolean.Value ? "true" : "false";
            case IntLiteralExpr integer:
                return TryFormatInteger(integer.Text, false, target);
            case FloatLiteralExpr floating:
                return TryFormatFloat(floating.Text, false, target);
            case UnaryMinusExpr { Operand: IntLiteralExpr integer }:
                return TryFormatInteger(integer.Text, true, target);
            case UnaryMinusExpr { Operand: FloatLiteralExpr floating }:
                return TryFormatFloat(floating.Text, true, target);
            default:
                return null;
        }
    }

    private static string? TryFormatInteger(string text, bool negative, CSharpType target)
    {
        if (!TryParseInteger(text, out var magnitude))
        {
            return null;
        }

        var value = negative ? -(decimal)magnitude : magnitude;
        if (target.Name == "double")
        {
            return (negative ? "-" : "") + magnitude.ToString(CultureInfo.InvariantCulture) + ".0";
        }

        if (!IntegerRanges.TryGetValue(target.Name, out var range) || value < range.Min || value > range.Max)
        {
            return null;
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseInteger(string text, out ulong magnitude)
    {
        text = text.Replace("_", string.Empty);
        var numberBase = 10;
        if (text.Length > 2 && text[0] == '0')
        {
            switch (text[1])
            {
                case 'x':
                case 'X':
                    numberBase = 16;
                    break;
                case 'b':
                case 'B':
                    numberBase = 2;
                    break;
                case 'o':
                case 'O':
                    numberBase = 8;
                    break;
            }

            if (numberBase != 10)
            {
                text = text.Substring(2);
            }
        }

        try
        {
            magnitude = Convert.ToUInt64(text, numberBase);
            return true;
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
        {
            magnitude = 0;
            return false;
        }
    }

    private static string? TryFormatFloat(string text, bool negative, CSharpType target)
    {
        if (target.Name != "double"
            || !double.TryParse(text.Replace("_", string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || double.IsInfinity(value))
        {
            return null;
        }

        var formatted = (negative ? -value : value).ToString("R", CultureInfo.InvariantCulture);
        if (formatted.IndexOfAny(new[] { '.', 'E' }) < 0)
        {
            formatted += ".0";
        }

        return formatted;
    }
}
