using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing.Syntax;

/// <param name="Parameters">Lambda-style parameters (<c>{ x, y -> ... }</c>) used by Listing/Mapping amendments.</param>
public sealed record ObjectBody(
    IReadOnlyList<Parameter> Parameters,
    IReadOnlyList<ObjectMember> Members,
    TextSpan Span) : SyntaxNode(Span);

public abstract record ObjectMember(TextSpan Span) : SyntaxNode(Span);

public sealed record ObjectElement(Expr Value, TextSpan Span) : ObjectMember(Span);

/// <summary><c>name = value</c>, <c>name: Type = value</c> or <c>name { ... }</c>.</summary>
public sealed record ObjectProperty(
    IReadOnlyList<Modifier> Modifiers,
    Identifier Name,
    TypeAnnotation? TypeAnnotation,
    Expr? Value,
    IReadOnlyList<ObjectBody> Bodies,
    TextSpan Span) : ObjectMember(Span);

public sealed record ObjectMethod(
    IReadOnlyList<Modifier> Modifiers,
    Identifier Name,
    TypeParameterList? TypeParameters,
    ParameterList Parameters,
    TypeAnnotation? ReturnType,
    Expr Body,
    TextSpan Span) : ObjectMember(Span);

/// <summary><c>[key] = value</c> or <c>[key] { ... }</c>.</summary>
public sealed record ObjectEntry(
    Expr Key,
    Expr? Value,
    IReadOnlyList<ObjectBody> Bodies,
    TextSpan Span) : ObjectMember(Span);

/// <summary><c>[[predicate]] = value</c> or <c>[[predicate]] { ... }</c>.</summary>
public sealed record MemberPredicate(
    Expr Predicate,
    Expr? Value,
    IReadOnlyList<ObjectBody> Bodies,
    TextSpan Span) : ObjectMember(Span);

public sealed record ObjectSpread(Expr Value, bool IsNullable, TextSpan Span) : ObjectMember(Span);

public sealed record WhenGenerator(Expr Condition, ObjectBody Body, ObjectBody? ElseBody, TextSpan Span)
    : ObjectMember(Span);

public sealed record ForGenerator(
    Parameter First,
    Parameter? Second,
    Expr Iterable,
    ObjectBody Body,
    TextSpan Span) : ObjectMember(Span);
