namespace PklGenerator.Parsing;

public enum TokenKind
{
    // keywords
    Abstract,
    Amends,
    As,
    Class,
    Const,
    Else,
    Extends,
    External,
    False,
    Fixed,
    For,
    Function,
    Hidden,
    If,
    Import,
    ImportStar,
    In,
    Is,
    Let,
    Local,
    Module,
    New,
    Nothing,
    Null,
    Open,
    Out,
    Outer,
    Read,
    ReadStar,
    ReadQuestion,
    Super,
    This,
    Throw,
    Trace,
    True,
    TypeAlias,
    Unknown,
    When,

    // reserved for future use
    Protected,
    Override,
    Record,
    Delete,
    Case,
    Switch,
    Vararg,

    // punctuation
    LParen,
    RParen,
    LBrace,
    RBrace,
    LBrack,
    RBrack,
    LPred,
    Comma,
    Dot,
    QDot,
    Coalesce,
    NonNull,
    At,
    Assign,
    Gt,
    Lt,

    // operators
    Not,
    Question,
    Colon,
    DColon,
    Arrow,
    Equal,
    NotEqual,
    Lte,
    Gte,
    And,
    Or,
    Plus,
    Minus,
    Pow,
    Star,
    Div,
    IntDiv,
    Mod,
    Union,
    Pipe,
    Spread,
    QSpread,
    Underscore,
    Eof,
    Semicolon,

    // literals and trivia
    Int,
    Float,
    Bin,
    Oct,
    Hex,
    Identifier,
    LineComment,
    BlockComment,
    DocComment,
    Shebang,

    // string pieces
    InterpolationStart,
    StringStart,
    StringMultiStart,
    StringNewline,
    StringEscapeNewline,
    StringEscapeTab,
    StringEscapeReturn,
    StringEscapeQuote,
    StringEscapeBackslash,
    StringEscapeUnicode,
    StringEscapeContinuation,
    StringEnd,
    StringPart,
}

public static class TokenKindExtensions
{
    public static bool IsModifier(this TokenKind kind) => kind is
        TokenKind.External or TokenKind.Abstract or TokenKind.Open or TokenKind.Local
        or TokenKind.Hidden or TokenKind.Fixed or TokenKind.Const;

    public static bool IsKeyword(this TokenKind kind) =>
        kind <= TokenKind.Vararg || kind == TokenKind.Underscore;

    /// <summary>Tokens the parser skips over between significant tokens.</summary>
    public static bool IsAffix(this TokenKind kind) =>
        kind is TokenKind.LineComment or TokenKind.BlockComment or TokenKind.Semicolon;

    public static string Text(this TokenKind kind) => kind switch
    {
        TokenKind.Underscore => "_",
        TokenKind.ImportStar => "import*",
        TokenKind.ReadStar => "read*",
        TokenKind.ReadQuestion => "read?",
        TokenKind.TypeAlias => "typealias",
        _ => kind.ToString().ToLowerInvariant(),
    };
}
