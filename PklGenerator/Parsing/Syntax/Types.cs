using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing.Syntax;

public abstract record TypeNode(TextSpan Span) : SyntaxNode(Span);

public sealed record UnknownType(TextSpan Span) : TypeNode(Span);

public sealed record NothingType(TextSpan Span) : TypeNode(Span);

public sealed record ModuleType(TextSpan Span) : TypeNode(Span);

public sealed record ThisType(TextSpan Span) : TypeNode(Span);

public sealed record DeclaredType(QualifiedIdentifier Name, TypeArgumentList? TypeArguments, TextSpan Span)
    : TypeNode(Span);

public sealed record StringLiteralType(StringConstant Value, TextSpan Span) : TypeNode(Span);

public sealed record NullableType(TypeNode Inner, TextSpan Span) : TypeNode(Span);

public sealed record ConstrainedType(TypeNode Inner, IReadOnlyList<Expr> Constraints, TextSpan Span)
    : TypeNode(Span);

public sealed record ParenthesizedType(TypeNode Inner, TextSpan Span) : TypeNode(Span);

/// <param name="DefaultIndex">Index of the member marked with <c>*</c>, or -1.</param>
public sealed record UnionType(IReadOnlyList<TypeNode> Members, int DefaultIndex, TextSpan Span)
    : TypeNode(Span);

public sealed record FunctionType(IReadOnlyList<TypeNode> Parameters, TypeNode ReturnType, TextSpan Span)
    : TypeNode(Span);
