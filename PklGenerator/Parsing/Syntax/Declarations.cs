using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Parsing.Syntax;

/// <summary>The doc comment, annotations and modifiers that precede a member.</summary>
public sealed record MemberHeader(
    DocComment? DocComment,
    IReadOnlyList<Annotation> Annotations,
    IReadOnlyList<Modifier> Modifiers)
{
    public bool HasModifier(ModifierKind kind) => Modifiers.Any(m => m.Kind == kind);
}

public enum ExtendsOrAmendsKind
{
    Extends,
    Amends,
}

public sealed record ExtendsOrAmendsClause(ExtendsOrAmendsKind Kind, StringConstant Url, TextSpan Span)
    : SyntaxNode(Span);

public sealed record ImportClause(StringConstant Url, bool IsGlob, Identifier? Alias, TextSpan Span)
    : SyntaxNode(Span);

public sealed record ModuleDecl(
    MemberHeader Header,
    QualifiedIdentifier? Name,
    ExtendsOrAmendsClause? ExtendsOrAmends,
    TextSpan Span) : SyntaxNode(Span);

public abstract record MemberDecl(MemberHeader Header, TextSpan Span) : SyntaxNode(Span);

public sealed record ClassDecl(
    MemberHeader Header,
    Identifier Name,
    TypeParameterList? TypeParameters,
    TypeNode? Superclass,
    ClassBody? Body,
    TextSpan Span) : MemberDecl(Header, Span);

public sealed record ClassBody(IReadOnlyList<MemberDecl> Members, TextSpan Span) : SyntaxNode(Span);

public sealed record ClassPropertyDecl(
    MemberHeader Header,
    Identifier Name,
    TypeAnnotation? TypeAnnotation,
    Expr? Value,
    IReadOnlyList<ObjectBody> Bodies,
    TextSpan Span) : MemberDecl(Header, Span);

public sealed record ClassMethodDecl(
    MemberHeader Header,
    Identifier Name,
    TypeParameterList? TypeParameters,
    ParameterList Parameters,
    TypeAnnotation? ReturnType,
    Expr? Body,
    TextSpan Span) : MemberDecl(Header, Span);

public sealed record TypeAliasDecl(
    MemberHeader Header,
    Identifier Name,
    TypeParameterList? TypeParameters,
    TypeNode Type,
    TextSpan Span) : MemberDecl(Header, Span);

/// <summary>A parsed Pkl module: an optional header, imports, and top level members.</summary>
public sealed record PklModule(
    ModuleDecl? ModuleDeclaration,
    IReadOnlyList<ImportClause> Imports,
    IReadOnlyList<MemberDecl> Members,
    TextSpan Span) : SyntaxNode(Span);
