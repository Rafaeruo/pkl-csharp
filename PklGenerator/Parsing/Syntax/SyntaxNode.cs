using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing.Syntax;

public abstract record SyntaxNode(TextSpan Span);

public sealed record Identifier(string Text, TextSpan Span) : SyntaxNode(Span)
{
    /// <summary>The identifier without the backticks of a quoted identifier.</summary>
    public string Name => Text.Length >= 2 && Text[0] == '`' ? Text.Substring(1, Text.Length - 2) : Text;
}

public sealed record QualifiedIdentifier(IReadOnlyList<Identifier> Parts, TextSpan Span) : SyntaxNode(Span)
{
    public Identifier Last => Parts[Parts.Count - 1];

    public override string ToString() => string.Join(".", Parts.Select(p => p.Name));
}

/// <summary>A string literal without interpolation, as used by import and amends clauses.</summary>
public sealed record StringConstant(string Value, TextSpan Span) : SyntaxNode(Span);

public enum ModifierKind
{
    External,
    Abstract,
    Open,
    Local,
    Hidden,
    Fixed,
    Const,
}

public sealed record Modifier(ModifierKind Kind, TextSpan Span) : SyntaxNode(Span);

/// <summary>The <c>///</c> documentation attached to a member.</summary>
public sealed record DocComment(IReadOnlyList<string> Lines, TextSpan Span) : SyntaxNode(Span)
{
    public string Text => string.Join("\n", Lines);
}

public sealed record Annotation(TypeNode Type, ObjectBody? Body, TextSpan Span) : SyntaxNode(Span);

public sealed record TypeAnnotation(TypeNode Type, TextSpan Span) : SyntaxNode(Span);

public enum Variance
{
    None,
    In,
    Out,
}

public sealed record TypeParameter(Variance Variance, Identifier Name, TextSpan Span) : SyntaxNode(Span);

public sealed record TypeParameterList(IReadOnlyList<TypeParameter> Parameters, TextSpan Span) : SyntaxNode(Span);

public sealed record TypeArgumentList(IReadOnlyList<TypeNode> Arguments, TextSpan Span) : SyntaxNode(Span);

public sealed record ArgumentList(IReadOnlyList<Expr> Arguments, TextSpan Span) : SyntaxNode(Span);

public abstract record Parameter(TextSpan Span) : SyntaxNode(Span);

public sealed record TypedIdentifier(Identifier Name, TypeAnnotation? TypeAnnotation, TextSpan Span) : Parameter(Span);

public sealed record UnderscoreParameter(TextSpan Span) : Parameter(Span);

public sealed record ParameterList(IReadOnlyList<Parameter> Parameters, TextSpan Span) : SyntaxNode(Span);
