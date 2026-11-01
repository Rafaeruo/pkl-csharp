using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing.Syntax;

public enum BinaryOperator
{
    NullCoalesce,
    Pipe,
    Or,
    And,
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,
    Plus,
    Minus,
    Multiply,
    Divide,
    IntegerDivide,
    Modulo,
    Power,
}

public enum ReadKind
{
    Read,
    ReadGlob,
    ReadOrNull,
}

public abstract record Expr(TextSpan Span) : SyntaxNode(Span);

public sealed record ThisExpr(TextSpan Span) : Expr(Span);

public sealed record OuterExpr(TextSpan Span) : Expr(Span);

public sealed record ModuleExpr(TextSpan Span) : Expr(Span);

public sealed record NullLiteralExpr(TextSpan Span) : Expr(Span);

public sealed record BoolLiteralExpr(bool Value, TextSpan Span) : Expr(Span);

/// <param name="Text">The literal as written, including any <c>0x</c>/<c>0b</c>/<c>0o</c> prefix and separators.</param>
public sealed record IntLiteralExpr(string Text, TextSpan Span) : Expr(Span);

public sealed record FloatLiteralExpr(string Text, TextSpan Span) : Expr(Span);

public abstract record StringPart(TextSpan Span) : SyntaxNode(Span);

/// <param name="Value">The text with escape sequences already resolved.</param>
public sealed record StringChars(string Value, TextSpan Span) : StringPart(Span);

public sealed record StringInterpolation(Expr Expression, TextSpan Span) : StringPart(Span);

public sealed record StringLiteralExpr(IReadOnlyList<StringPart> Parts, bool IsMultiLine, TextSpan Span)
    : Expr(Span)
{
    /// <summary>The value when the string has no interpolation, otherwise null.</summary>
    public string? ConstantValue => Parts.All(p => p is StringChars)
        ? string.Concat(Parts.Cast<StringChars>().Select(p => p.Value))
        : null;
}

public sealed record ThrowExpr(Expr Message, TextSpan Span) : Expr(Span);

public sealed record TraceExpr(Expr Value, TextSpan Span) : Expr(Span);

public sealed record ImportExpr(StringConstant Url, bool IsGlob, TextSpan Span) : Expr(Span);

public sealed record ReadExpr(Expr Resource, ReadKind Kind, TextSpan Span) : Expr(Span);

/// <param name="Type">Null when the type is inferred from context (<c>new { ... }</c>).</param>
public sealed record NewExpr(TypeNode? Type, ObjectBody Body, TextSpan Span) : Expr(Span);

public sealed record AmendsExpr(Expr Target, ObjectBody Body, TextSpan Span) : Expr(Span);

public sealed record UnaryMinusExpr(Expr Operand, TextSpan Span) : Expr(Span);

public sealed record LogicalNotExpr(Expr Operand, TextSpan Span) : Expr(Span);

public sealed record NonNullExpr(Expr Operand, TextSpan Span) : Expr(Span);

public sealed record ParenthesizedExpr(Expr Inner, TextSpan Span) : Expr(Span);

public sealed record FunctionLiteralExpr(ParameterList Parameters, Expr Body, TextSpan Span) : Expr(Span);

public sealed record SuperAccessExpr(
    Identifier Name,
    TypeArgumentList? TypeArguments,
    ArgumentList? Arguments,
    TextSpan Span) : Expr(Span);

public sealed record SuperSubscriptExpr(Expr Index, TextSpan Span) : Expr(Span);

public sealed record UnqualifiedAccessExpr(
    Identifier Name,
    TypeArgumentList? TypeArguments,
    ArgumentList? Arguments,
    TextSpan Span) : Expr(Span);

public sealed record QualifiedAccessExpr(
    Expr Receiver,
    Identifier Name,
    bool IsNullable,
    TypeArgumentList? TypeArguments,
    ArgumentList? Arguments,
    TextSpan Span) : Expr(Span);

public sealed record SubscriptExpr(Expr Receiver, Expr Index, TextSpan Span) : Expr(Span);

public sealed record BinaryOperatorExpr(Expr Left, BinaryOperator Operator, Expr Right, TextSpan Span) : Expr(Span);

public sealed record TypeCheckExpr(Expr Operand, TypeNode Type, TextSpan Span) : Expr(Span);

public sealed record TypeCastExpr(Expr Operand, TypeNode Type, TextSpan Span) : Expr(Span);

public sealed record IfExpr(Expr Condition, Expr Then, Expr Else, TextSpan Span) : Expr(Span);

public sealed record LetExpr(Parameter Parameter, Expr Binding, Expr Body, TextSpan Span) : Expr(Span);
