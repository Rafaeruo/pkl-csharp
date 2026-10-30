using System.Text;
using Microsoft.CodeAnalysis.Text;
using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Parsing;

/// <summary>
/// A recursive descent parser for the Pkl language. It follows the structure of the reference
/// parser (pkl-parser's ParserImpl.java) so the accepted grammar matches Pkl's.
/// The first syntax error throws a <see cref="PklParseException"/>.
/// </summary>
public sealed class Parser
{
    private const int MaxDepth = 128;

    private readonly string _source;
    private readonly List<Token> _raw;

    // Indexes into _raw of the tokens the parser sees, i.e. everything but comments and semicolons.
    private readonly List<int> _significant = new();
    // For each significant token: was it preceded by a semicolon, and how many line breaks precede it.
    private readonly List<bool> _precededBySemicolon = new();
    private readonly List<int> _newLinesBefore = new();
    // For each raw token: position of the first significant token at or after it.
    private readonly int[] _significantAfter;

    private int _pos;
    private int _depth;

    private Parser(string source)
    {
        _source = source;
        _raw = Lexer.Tokenize(source);
        _significantAfter = new int[_raw.Count];

        var semicolon = false;
        var newLines = 0;
        for (var i = 0; i < _raw.Count; i++)
        {
            var token = _raw[i];
            newLines += token.NewLinesBetween;
            if (token.Kind.IsAffix())
            {
                semicolon |= token.Kind == TokenKind.Semicolon;
                continue;
            }

            _significant.Add(i);
            _precededBySemicolon.Add(semicolon);
            _newLinesBefore.Add(newLines);
            semicolon = false;
            newLines = 0;
        }

        var nextSignificant = _significant.Count - 1;
        var position = _significant.Count - 1;
        for (var i = _raw.Count - 1; i >= 0; i--)
        {
            if (position >= 0 && _significant[position] == i)
            {
                nextSignificant = position--;
            }

            _significantAfter[i] = nextSignificant;
        }
    }

    public static PklModule ParseModule(string source) => new Parser(source).ParseModuleCore();

    public static Expr ParseExpression(string source)
    {
        var parser = new Parser(source);
        var expr = parser.ParseExpr();
        parser.Expect(TokenKind.Eof, "end of file");
        return expr;
    }

    #region Token access

    private Token Current => _raw[_significant[_pos]];
    private TokenKind Lookahead => Current.Kind;
    private TextSpan LookaheadSpan => Current.Span;
    private bool PrecededBySemicolon => _precededBySemicolon[_pos];
    private int NewLinesBeforeLookahead => _newLinesBefore[_pos];

    /// <summary>True when the lookahead starts on the same line as the previous token and wasn't separated by `;`.</summary>
    private bool OnSameLine => !PrecededBySemicolon && NewLinesBeforeLookahead == 0;

    private Token Next()
    {
        var token = Current;
        if (_pos < _significant.Count - 1)
        {
            _pos++;
        }

        return token;
    }

    /// <summary>Steps back to the previous token. Only valid directly after consuming a token.</summary>
    private void Backtrack() => _pos--;

    private string TextOf(Token token) => _source.Substring(token.Span.Start, token.Span.Length);

    private string LookaheadText => Lookahead == TokenKind.Eof ? "EOF" : TextOf(Current);

    private TextSpan PreviousEnd()
    {
        if (_pos == 0)
        {
            return new TextSpan(0, 0);
        }

        var end = _raw[_significant[_pos - 1]].Span.End;
        return new TextSpan(end, 0);
    }

    private Token Expect(TokenKind kind, string expected)
    {
        if (Lookahead == kind)
        {
            return Next();
        }

        var span = LookaheadSpan;
        if (Lookahead == TokenKind.Eof || NewLinesBeforeLookahead > 0)
        {
            // point at the end of the last token rather than at EOF or the next line
            span = PreviousEnd();
        }

        throw new PklParseException($"Unexpected token `{LookaheadText}`, expected `{expected}`", span);
    }

    private Token Expect(TokenKind kind, string expected1, string expected2)
    {
        if (Lookahead == kind)
        {
            return Next();
        }

        var span = LookaheadSpan;
        if (Lookahead == TokenKind.Eof || NewLinesBeforeLookahead > 0)
        {
            span = PreviousEnd();
        }

        throw new PklParseException(
            $"Unexpected token `{LookaheadText}`, expected `{expected1}` or `{expected2}`", span);
    }

    private PklParseException Error(string message) => new(message, LookaheadSpan);

    private PklParseException UnexpectedToken(string expected) =>
        Error($"Unexpected token `{LookaheadText}`, expected `{expected}`");

    private static TextSpan Through(TextSpan start, TextSpan end) => TextSpan.FromBounds(start.Start, end.End);

    private T Nested<T>(Func<T> parse)
    {
        if (++_depth > MaxDepth)
        {
            throw Error("Too deeply nested");
        }

        try
        {
            return parse();
        }
        finally
        {
            _depth--;
        }
    }

    #endregion

    #region Module

    private PklModule ParseModuleCore()
    {
        if (Lookahead == TokenKind.Eof)
        {
            return new PklModule(null, Array.Empty<ImportClause>(), Array.Empty<MemberDecl>(), new TextSpan(0, 0));
        }

        if (Lookahead == TokenKind.Shebang)
        {
            Next();
        }

        var start = LookaheadSpan;
        var imports = new List<ImportClause>();
        var members = new List<MemberDecl>();

        var header = ParseMemberHeader();
        var moduleDecl = ParseModuleDecl(header);
        var headerPending = moduleDecl == null && HasContent(header);

        while (Lookahead is TokenKind.Import or TokenKind.ImportStar)
        {
            if (headerPending)
            {
                throw Error("Imports cannot have doc comments, annotations or modifiers");
            }

            imports.Add(ParseImportDecl());
        }

        if (headerPending)
        {
            members.Add(ParseModuleMember(header));
        }

        while (Lookahead != TokenKind.Eof)
        {
            if (Lookahead is TokenKind.Import or TokenKind.ImportStar)
            {
                throw Error("Imports must come before all other members");
            }

            members.Add(ParseModuleMember(ParseMemberHeader()));
        }

        return new PklModule(moduleDecl, imports, members, TextSpan.FromBounds(start.Start, LookaheadSpan.End));
    }

    private static bool HasContent(MemberHeader header) =>
        header.DocComment != null || header.Annotations.Count > 0 || header.Modifiers.Count > 0;

    private static TextSpan HeaderStart(MemberHeader header, TextSpan fallback)
    {
        if (header.DocComment != null)
        {
            return header.DocComment.Span;
        }

        if (header.Annotations.Count > 0)
        {
            return header.Annotations[0].Span;
        }

        if (header.Modifiers.Count > 0)
        {
            return header.Modifiers[0].Span;
        }

        return fallback;
    }

    private ModuleDecl? ParseModuleDecl(MemberHeader header)
    {
        QualifiedIdentifier? name = null;
        TextSpan? start = HasContent(header) ? HeaderStart(header, default) : null;
        TextSpan end = default;

        if (Lookahead == TokenKind.Module)
        {
            var moduleKeyword = Next().Span;
            start ??= moduleKeyword;
            name = ParseQualifiedIdentifier();
            end = name.Span;
        }

        var clause = ParseExtendsOrAmends();
        if (clause != null)
        {
            start ??= clause.Span;
            end = clause.Span;
        }

        if (name == null && clause == null)
        {
            return null;
        }

        return new ModuleDecl(header, name, clause, Through(start!.Value, end));
    }

    private ExtendsOrAmendsClause? ParseExtendsOrAmends()
    {
        ExtendsOrAmendsKind kind;
        if (Lookahead == TokenKind.Extends)
        {
            kind = ExtendsOrAmendsKind.Extends;
        }
        else if (Lookahead == TokenKind.Amends)
        {
            kind = ExtendsOrAmendsKind.Amends;
        }
        else
        {
            return null;
        }

        var keyword = Next().Span;
        var url = ParseStringConstant();
        return new ExtendsOrAmendsClause(kind, url, Through(keyword, url.Span));
    }

    private ImportClause ParseImportDecl()
    {
        TextSpan start;
        var isGlob = false;
        if (Lookahead == TokenKind.ImportStar)
        {
            start = Next().Span;
            isGlob = true;
        }
        else
        {
            start = Expect(TokenKind.Import, "import", "import*").Span;
        }

        var url = ParseStringConstant();
        var end = url.Span;
        Identifier? alias = null;
        if (Lookahead == TokenKind.As)
        {
            Next();
            alias = ParseIdentifier();
            end = alias.Span;
        }

        return new ImportClause(url, isGlob, alias, Through(start, end));
    }

    private MemberDecl ParseModuleMember(MemberHeader header)
    {
        switch (Lookahead)
        {
            case TokenKind.Identifier:
                return ParseClassProperty(header);
            case TokenKind.TypeAlias:
                return ParseTypeAlias(header);
            case TokenKind.Class:
                return ParseClass(header);
            case TokenKind.Function:
                return ParseClassMethod(header);
            case TokenKind.Eof:
                throw Error("Unexpected end of file");
            default:
                if (Lookahead.IsKeyword())
                {
                    throw Error($"Keyword `{Lookahead.Text()}` is not allowed here");
                }

                if (Lookahead == TokenKind.DocComment)
                {
                    throw Error("Dangling doc comment");
                }

                throw Error($"Invalid top level token `{LookaheadText}`");
        }
    }

    #endregion

    #region Member headers

    private MemberHeader ParseMemberHeader()
    {
        DocComment? docComment = null;
        var annotations = new List<Annotation>();
        var modifiers = new List<Modifier>();

        if (Lookahead == TokenKind.DocComment)
        {
            docComment = ParseDocComment();
        }

        while (Lookahead == TokenKind.At)
        {
            annotations.Add(ParseAnnotation());
        }

        while (Lookahead.IsModifier())
        {
            modifiers.Add(ParseModifier());
        }

        return new MemberHeader(docComment, annotations, modifiers);
    }

    private DocComment ParseDocComment()
    {
        // Doc comment lines are only separated from the member they document by other comments;
        // a blank line ends the doc comment. This walks raw tokens so comments aren't skipped.
        var spans = new List<TextSpan> { Current.Span };
        var r = _significant[_pos] + 1;

        while (true)
        {
            r = SkipSemicolons(r);
            var token = _raw[r];
            if (token.Kind is not (TokenKind.DocComment or TokenKind.LineComment or TokenKind.BlockComment))
            {
                break;
            }

            r++;
            if (token.NewLinesBetween > 1)
            {
                if (token.Kind == TokenKind.DocComment)
                {
                    // starts a new doc comment that belongs to whatever follows
                    r--;
                }

                break;
            }

            if (token.Kind == TokenKind.DocComment)
            {
                spans.Add(token.Span);
            }
        }

        while (true)
        {
            r = SkipSemicolons(r);
            if (_raw[r].Kind is TokenKind.LineComment or TokenKind.BlockComment)
            {
                r++;
            }
            else
            {
                break;
            }
        }

        _pos = _significantAfter[r];

        var lines = spans.Select(span =>
        {
            var text = _source.Substring(span.Start, span.Length);
            text = text.Substring(3);
            return text.StartsWith(" ", StringComparison.Ordinal) ? text.Substring(1) : text;
        }).ToList();

        return new DocComment(lines, Through(spans[0], spans[spans.Count - 1]));
    }

    private int SkipSemicolons(int rawIndex)
    {
        while (_raw[rawIndex].Kind == TokenKind.Semicolon)
        {
            rawIndex++;
        }

        return rawIndex;
    }

    private Annotation ParseAnnotation()
    {
        var start = Next().Span;
        var type = ParseType();
        ObjectBody? body = null;
        var end = type.Span;
        if (Lookahead == TokenKind.LBrace)
        {
            body = ParseObjectBody();
            end = body.Span;
        }

        return new Annotation(type, body, Through(start, end));
    }

    private Modifier ParseModifier()
    {
        var kind = Lookahead switch
        {
            TokenKind.External => ModifierKind.External,
            TokenKind.Abstract => ModifierKind.Abstract,
            TokenKind.Open => ModifierKind.Open,
            TokenKind.Local => ModifierKind.Local,
            TokenKind.Hidden => ModifierKind.Hidden,
            TokenKind.Fixed => ModifierKind.Fixed,
            TokenKind.Const => ModifierKind.Const,
            _ => throw Error($"Expected a modifier but found `{LookaheadText}`"),
        };

        return new Modifier(kind, Next().Span);
    }

    private List<Modifier> ParseModifierList()
    {
        var modifiers = new List<Modifier>();
        while (Lookahead.IsModifier())
        {
            modifiers.Add(ParseModifier());
        }

        return modifiers;
    }

    #endregion

    #region Declarations

    private TypeAliasDecl ParseTypeAlias(MemberHeader header)
    {
        var keyword = Next().Span;
        var start = HeaderStart(header, keyword);
        var name = ParseIdentifier();
        TypeParameterList? typeParameters = null;
        if (Lookahead == TokenKind.Lt)
        {
            typeParameters = ParseTypeParameterList(true);
        }

        Expect(TokenKind.Assign, "=");
        var type = ParseType();
        return new TypeAliasDecl(header, name, typeParameters, type, Through(start, type.Span));
    }

    private ClassDecl ParseClass(MemberHeader header)
    {
        var keyword = Next().Span;
        var start = HeaderStart(header, keyword);
        var name = ParseIdentifier();
        var end = name.Span;

        TypeParameterList? typeParameters = null;
        if (Lookahead == TokenKind.Lt)
        {
            typeParameters = ParseTypeParameterList(true);
            end = typeParameters.Span;
        }

        TypeNode? superclass = null;
        if (Lookahead == TokenKind.Extends)
        {
            Next();
            superclass = ParseType();
            end = superclass.Span;
        }

        ClassBody? body = null;
        if (Lookahead == TokenKind.LBrace)
        {
            body = ParseClassBody();
            end = body.Span;
        }

        return new ClassDecl(header, name, typeParameters, superclass, body, Through(start, end));
    }

    private ClassBody ParseClassBody()
    {
        var start = Expect(TokenKind.LBrace, "{").Span;
        var members = new List<MemberDecl>();
        while (Lookahead != TokenKind.RBrace && Lookahead != TokenKind.Eof)
        {
            var header = ParseMemberHeader();
            members.Add(Lookahead == TokenKind.Function ? ParseClassMethod(header) : ParseClassProperty(header));
        }

        if (Lookahead == TokenKind.Eof)
        {
            throw new PklParseException("Missing closing delimiter `}`", PreviousEnd());
        }

        var end = Expect(TokenKind.RBrace, "}").Span;
        return new ClassBody(members, Through(start, end));
    }

    private ClassPropertyDecl ParseClassProperty(MemberHeader header)
    {
        var name = ParseIdentifier();
        var start = HeaderStart(header, name.Span);

        TypeAnnotation? typeAnnotation = null;
        Expr? value = null;
        var bodies = new List<ObjectBody>();

        if (Lookahead == TokenKind.Colon)
        {
            typeAnnotation = ParseTypeAnnotation();
        }

        if (Lookahead == TokenKind.Assign)
        {
            Next();
            value = ParseExpr();
        }
        else if (Lookahead == TokenKind.LBrace)
        {
            if (typeAnnotation != null)
            {
                throw Error("Type annotations are not allowed on property amendments");
            }

            while (Lookahead == TokenKind.LBrace)
            {
                bodies.Add(ParseObjectBody());
            }
        }

        TextSpan end;
        if (value != null)
        {
            end = value.Span;
        }
        else if (bodies.Count > 0)
        {
            end = bodies[bodies.Count - 1].Span;
        }
        else if (typeAnnotation != null)
        {
            end = typeAnnotation.Span;
        }
        else
        {
            throw new PklParseException("A property must have a type annotation or a value", name.Span);
        }

        return new ClassPropertyDecl(header, name, typeAnnotation, value, bodies, Through(start, end));
    }

    private ClassMethodDecl ParseClassMethod(MemberHeader header)
    {
        var keyword = Expect(TokenKind.Function, "function").Span;
        var start = HeaderStart(header, keyword);
        var name = ParseIdentifier();

        TypeParameterList? typeParameters = null;
        if (Lookahead == TokenKind.Lt)
        {
            typeParameters = ParseTypeParameterList(false);
        }

        var parameters = ParseParameterList();
        var end = parameters.Span;

        TypeAnnotation? returnType = null;
        if (Lookahead == TokenKind.Colon)
        {
            returnType = ParseTypeAnnotation();
            end = returnType.Span;
        }

        Expr? body = null;
        if (Lookahead == TokenKind.Assign)
        {
            Next();
            body = ParseExpr();
            end = body.Span;
        }

        return new ClassMethodDecl(header, name, typeParameters, parameters, returnType, body, Through(start, end));
    }

    #endregion

    #region Object bodies

    private ObjectBody ParseObjectBody() => Nested(ParseObjectBodyCore);

    private ObjectBody ParseObjectBodyCore()
    {
        var start = Expect(TokenKind.LBrace, "{").Span;
        var parameters = new List<Parameter>();
        var members = new List<ObjectMember>();

        if (Lookahead == TokenKind.RBrace)
        {
            return new ObjectBody(parameters, members, Through(start, Next().Span));
        }

        if (Lookahead == TokenKind.Underscore)
        {
            // it's a parameter
            parameters.AddRange(ParseListOfParameter(TokenKind.Comma, TokenKind.Arrow));
            Expect(TokenKind.Arrow, ",", "->");
        }
        else if (Lookahead == TokenKind.Identifier)
        {
            // not sure yet whether this is a parameter list or a member
            var identifier = ParseIdentifier();
            if (Lookahead == TokenKind.Arrow)
            {
                Next();
                parameters.Add(new TypedIdentifier(identifier, null, identifier.Span));
            }
            else if (Lookahead == TokenKind.Comma)
            {
                Backtrack();
                parameters.AddRange(ParseListOfParameter(TokenKind.Comma, TokenKind.Arrow));
                Expect(TokenKind.Arrow, ",", "->");
            }
            else if (Lookahead == TokenKind.Colon)
            {
                var annotation = ParseTypeAnnotation();
                var typed = new TypedIdentifier(identifier, annotation, Through(identifier.Span, annotation.Span));
                if (Lookahead == TokenKind.Comma)
                {
                    Next();
                    parameters.Add(typed);
                    parameters.AddRange(ParseListOfParameter(TokenKind.Comma, TokenKind.Arrow));
                    Expect(TokenKind.Arrow, ",", "->");
                }
                else if (Lookahead == TokenKind.Arrow)
                {
                    Next();
                    parameters.Add(typed);
                }
                else
                {
                    // it's a typed property
                    Expect(TokenKind.Assign, "=");
                    var value = ParseExpr();
                    members.Add(new ObjectProperty(
                        Array.Empty<Modifier>(),
                        identifier,
                        annotation,
                        value,
                        Array.Empty<ObjectBody>(),
                        Through(identifier.Span, value.Span)));
                }
            }
            else
            {
                Backtrack();
            }
        }

        while (Lookahead != TokenKind.RBrace)
        {
            if (Lookahead == TokenKind.Eof)
            {
                throw new PklParseException("Missing closing delimiter `}`", PreviousEnd());
            }

            members.Add(ParseObjectMember());
        }

        var end = Next().Span;
        return new ObjectBody(parameters, members, Through(start, end));
    }

    private ObjectMember ParseObjectMember()
    {
        switch (Lookahead)
        {
            case TokenKind.Identifier:
                Next();
                var isProperty = Lookahead is TokenKind.LBrace or TokenKind.Colon or TokenKind.Assign;
                Backtrack();
                return isProperty ? ParseObjectProperty(null) : ParseObjectElement();
            case TokenKind.Function:
                return ParseObjectMethod(new List<Modifier>());
            case TokenKind.LPred:
                return ParseMemberPredicate();
            case TokenKind.LBrack:
                return ParseObjectEntry();
            case TokenKind.Spread:
            case TokenKind.QSpread:
                return ParseObjectSpread();
            case TokenKind.When:
                return ParseWhenGenerator();
            case TokenKind.For:
                return ParseForGenerator();
            case TokenKind.TypeAlias:
            case TokenKind.Class:
                throw new PklParseException("Missing closing delimiter `}`", PreviousEnd());
            default:
                var modifiers = ParseModifierList();
                if (modifiers.Count == 0)
                {
                    return ParseObjectElement();
                }

                return Lookahead == TokenKind.Function ? ParseObjectMethod(modifiers) : ParseObjectProperty(modifiers);
        }
    }

    private ObjectElement ParseObjectElement()
    {
        var value = ParseExpr("}");
        return new ObjectElement(value, value.Span);
    }

    private ObjectProperty ParseObjectProperty(List<Modifier>? modifiers)
    {
        modifiers ??= ParseModifierList();
        var start = modifiers.Count > 0 ? modifiers[0].Span : LookaheadSpan;
        var name = ParseIdentifier();
        TypeAnnotation? typeAnnotation = null;
        if (Lookahead == TokenKind.Colon)
        {
            typeAnnotation = ParseTypeAnnotation();
        }

        if (typeAnnotation != null || Lookahead == TokenKind.Assign)
        {
            Expect(TokenKind.Assign, "=");
            var value = ParseExpr("}");
            return new ObjectProperty(modifiers, name, typeAnnotation, value, Array.Empty<ObjectBody>(),
                Through(start, value.Span));
        }

        var bodies = ParseBodyList();
        return new ObjectProperty(modifiers, name, null, null, bodies,
            Through(start, bodies[bodies.Count - 1].Span));
    }

    private ObjectMethod ParseObjectMethod(List<Modifier> modifiers)
    {
        var start = modifiers.Count > 0 ? modifiers[0].Span : LookaheadSpan;
        Expect(TokenKind.Function, "function");
        var name = ParseIdentifier();
        TypeParameterList? typeParameters = null;
        if (Lookahead == TokenKind.Lt)
        {
            typeParameters = ParseTypeParameterList(false);
        }

        var parameters = ParseParameterList();
        TypeAnnotation? returnType = null;
        if (Lookahead == TokenKind.Colon)
        {
            returnType = ParseTypeAnnotation();
        }

        Expect(TokenKind.Assign, "=");
        var body = ParseExpr("}");
        return new ObjectMethod(modifiers, name, typeParameters, parameters, returnType, body,
            Through(start, body.Span));
    }

    private MemberPredicate ParseMemberPredicate()
    {
        var start = Next().Span;
        var predicate = ParseExpr("]]");
        var firstBracket = Expect(TokenKind.RBrack, "]]").Span;
        if (Lookahead != TokenKind.RBrack)
        {
            throw new PklParseException($"Unexpected token `{LookaheadText}`, expected `]]`", firstBracket);
        }

        var secondBracket = Next().Span;
        if (firstBracket.Start != secondBracket.Start - 1)
        {
            // there must not be whitespace between the two `]`
            throw new PklParseException("Unexpected token `] ]`, expected `]]`", firstBracket);
        }

        if (Lookahead == TokenKind.Assign)
        {
            Next();
            var value = ParseExpr("}");
            return new MemberPredicate(predicate, value, Array.Empty<ObjectBody>(), Through(start, value.Span));
        }

        var bodies = ParseBodyList();
        return new MemberPredicate(predicate, null, bodies, Through(start, bodies[bodies.Count - 1].Span));
    }

    private ObjectEntry ParseObjectEntry()
    {
        var start = Expect(TokenKind.LBrack, "[").Span;
        var key = ParseExpr("]");
        Expect(TokenKind.RBrack, "]");
        if (Lookahead == TokenKind.Assign)
        {
            Next();
            var value = ParseExpr("}");
            return new ObjectEntry(key, value, Array.Empty<ObjectBody>(), Through(start, value.Span));
        }

        var bodies = ParseBodyList();
        return new ObjectEntry(key, null, bodies, Through(start, bodies[bodies.Count - 1].Span));
    }

    private ObjectSpread ParseObjectSpread()
    {
        var start = Next();
        var value = ParseExpr("}");
        return new ObjectSpread(value, start.Kind == TokenKind.QSpread, Through(start.Span, value.Span));
    }

    private WhenGenerator ParseWhenGenerator()
    {
        var start = Next().Span;
        Expect(TokenKind.LParen, "(");
        var condition = ParseExpr(")");
        Expect(TokenKind.RParen, ")");
        var body = ParseObjectBody();
        var end = body.Span;
        ObjectBody? elseBody = null;
        if (Lookahead == TokenKind.Else)
        {
            Next();
            elseBody = ParseObjectBody();
            end = elseBody.Span;
        }

        return new WhenGenerator(condition, body, elseBody, Through(start, end));
    }

    private ForGenerator ParseForGenerator()
    {
        var start = Next().Span;
        Expect(TokenKind.LParen, "(");
        var first = ParseParameter();
        Parameter? second = null;
        if (Lookahead == TokenKind.Comma)
        {
            Next();
            second = ParseParameter();
        }

        Expect(TokenKind.In, "in");
        var iterable = ParseExpr(")");
        Expect(TokenKind.RParen, ")");
        var body = ParseObjectBody();
        return new ForGenerator(first, second, iterable, body, Through(start, body.Span));
    }

    private List<ObjectBody> ParseBodyList()
    {
        if (Lookahead != TokenKind.LBrace)
        {
            throw Error($"Unexpected token `{LookaheadText}`, expected `{{` or `=`");
        }

        var bodies = new List<ObjectBody>();
        do
        {
            bodies.Add(ParseObjectBody());
        } while (Lookahead == TokenKind.LBrace);

        return bodies;
    }

    #endregion

    #region Expressions

    private Expr ParseExpr(string? expectation = null) => Nested(() => ParseExpr(expectation, 1));

    private Expr ParseExpr(string? expectation, int minPrecedence)
    {
        var expr = ParseExprAtom(expectation);
        while (TryGetOperator(Lookahead, out var precedence, out var isLeftAssoc))
        {
            if (precedence < minPrecedence)
            {
                break;
            }

            // `-` must be on the same line as the left operand and not preceded by `;`
            if (Lookahead == TokenKind.Minus && !OnSameLine)
            {
                break;
            }

            var op = Next();
            switch (op.Kind)
            {
                case TokenKind.Is:
                {
                    var type = ParseType();
                    expr = new TypeCheckExpr(expr, type, Through(expr.Span, type.Span));
                    break;
                }
                case TokenKind.As:
                {
                    var type = ParseType();
                    expr = new TypeCastExpr(expr, type, Through(expr.Span, type.Span));
                    break;
                }
                case TokenKind.Dot:
                case TokenKind.QDot:
                    expr = ParseMemberAccess(expr, op.Kind == TokenKind.QDot);
                    break;
                default:
                    var nextMinPrecedence = isLeftAssoc ? precedence + 1 : precedence;
                    var right = ParseExpr(expectation, nextMinPrecedence);
                    expr = new BinaryOperatorExpr(expr, ToBinaryOperator(op.Kind), right, Through(expr.Span, right.Span));
                    break;
            }
        }

        return expr;
    }

    private static bool TryGetOperator(TokenKind kind, out int precedence, out bool isLeftAssoc)
    {
        isLeftAssoc = true;
        switch (kind)
        {
            case TokenKind.Coalesce:
                precedence = 1;
                isLeftAssoc = false;
                return true;
            case TokenKind.Pipe:
                precedence = 2;
                return true;
            case TokenKind.Or:
                precedence = 3;
                return true;
            case TokenKind.And:
                precedence = 4;
                return true;
            case TokenKind.Equal:
            case TokenKind.NotEqual:
                precedence = 5;
                return true;
            case TokenKind.Is:
            case TokenKind.As:
                precedence = 6;
                return true;
            case TokenKind.Lt:
            case TokenKind.Gt:
            case TokenKind.Lte:
            case TokenKind.Gte:
                precedence = 7;
                return true;
            case TokenKind.Plus:
            case TokenKind.Minus:
                precedence = 8;
                return true;
            case TokenKind.Star:
            case TokenKind.Div:
            case TokenKind.IntDiv:
            case TokenKind.Mod:
                precedence = 9;
                return true;
            case TokenKind.Pow:
                precedence = 10;
                isLeftAssoc = false;
                return true;
            case TokenKind.Dot:
            case TokenKind.QDot:
                precedence = 20;
                return true;
            default:
                precedence = 0;
                return false;
        }
    }

    private static BinaryOperator ToBinaryOperator(TokenKind kind) => kind switch
    {
        TokenKind.Coalesce => BinaryOperator.NullCoalesce,
        TokenKind.Pipe => BinaryOperator.Pipe,
        TokenKind.Or => BinaryOperator.Or,
        TokenKind.And => BinaryOperator.And,
        TokenKind.Equal => BinaryOperator.Equal,
        TokenKind.NotEqual => BinaryOperator.NotEqual,
        TokenKind.Lt => BinaryOperator.LessThan,
        TokenKind.Gt => BinaryOperator.GreaterThan,
        TokenKind.Lte => BinaryOperator.LessThanOrEqual,
        TokenKind.Gte => BinaryOperator.GreaterThanOrEqual,
        TokenKind.Plus => BinaryOperator.Plus,
        TokenKind.Minus => BinaryOperator.Minus,
        TokenKind.Star => BinaryOperator.Multiply,
        TokenKind.Div => BinaryOperator.Divide,
        TokenKind.IntDiv => BinaryOperator.IntegerDivide,
        TokenKind.Mod => BinaryOperator.Modulo,
        TokenKind.Pow => BinaryOperator.Power,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Parses what follows a `.` or `?.`: the member name and optional type/value arguments.</summary>
    private QualifiedAccessExpr ParseMemberAccess(Expr receiver, bool isNullable)
    {
        var name = ParseIdentifier();
        var typeArguments = ParseOptionalTypeArguments();
        ArgumentList? arguments = null;
        if (Lookahead == TokenKind.LParen && OnSameLine)
        {
            arguments = ParseArgumentList();
        }
        else if (typeArguments != null)
        {
            throw UnexpectedToken("(");
        }

        var end = arguments?.Span ?? name.Span;
        return new QualifiedAccessExpr(receiver, name, isNullable, typeArguments, arguments, Through(receiver.Span, end));
    }

    private TypeArgumentList? ParseOptionalTypeArguments()
    {
        if (Lookahead == TokenKind.DColon && OnSameLine)
        {
            Next();
            return ParseTypeArgumentList();
        }

        return null;
    }

    private Expr ParseExprAtom(string? expectation)
    {
        Expr expr;
        switch (Lookahead)
        {
            case TokenKind.This:
                expr = new ThisExpr(Next().Span);
                break;
            case TokenKind.Outer:
                expr = new OuterExpr(Next().Span);
                break;
            case TokenKind.Module:
                expr = new ModuleExpr(Next().Span);
                break;
            case TokenKind.Null:
                expr = new NullLiteralExpr(Next().Span);
                break;
            case TokenKind.Throw:
            {
                var start = Next().Span;
                Expect(TokenKind.LParen, "(");
                var message = ParseExpr(")");
                var end = Expect(TokenKind.RParen, ")").Span;
                expr = new ThrowExpr(message, Through(start, end));
                break;
            }
            case TokenKind.Trace:
            {
                var start = Next().Span;
                Expect(TokenKind.LParen, "(");
                var value = ParseExpr(")");
                var end = Expect(TokenKind.RParen, ")").Span;
                expr = new TraceExpr(value, Through(start, end));
                break;
            }
            case TokenKind.Import:
            case TokenKind.ImportStar:
            {
                var start = Next();
                Expect(TokenKind.LParen, "(");
                var url = ParseStringConstant();
                var end = Expect(TokenKind.RParen, ")").Span;
                expr = new ImportExpr(url, start.Kind == TokenKind.ImportStar, Through(start.Span, end));
                break;
            }
            case TokenKind.Read:
            case TokenKind.ReadStar:
            case TokenKind.ReadQuestion:
            {
                var start = Next();
                var kind = start.Kind switch
                {
                    TokenKind.ReadQuestion => ReadKind.ReadOrNull,
                    TokenKind.ReadStar => ReadKind.ReadGlob,
                    _ => ReadKind.Read,
                };
                Expect(TokenKind.LParen, "(");
                var resource = ParseExpr(")");
                var end = Expect(TokenKind.RParen, ")").Span;
                expr = new ReadExpr(resource, kind, Through(start.Span, end));
                break;
            }
            case TokenKind.New:
            {
                var start = Next().Span;
                TypeNode? type = null;
                if (Lookahead != TokenKind.LBrace)
                {
                    type = ParseType("{");
                }

                var body = ParseObjectBody();
                expr = new NewExpr(type, body, Through(start, body.Span));
                break;
            }
            case TokenKind.Minus:
            {
                var start = Next().Span;
                // unary minus binds tighter than any binary operator
                var operand = ParseExprAtom(expectation);
                expr = new UnaryMinusExpr(operand, Through(start, operand.Span));
                break;
            }
            case TokenKind.Not:
            {
                var start = Next().Span;
                var operand = ParseExprAtom(expectation);
                expr = new LogicalNotExpr(operand, Through(start, operand.Span));
                break;
            }
            case TokenKind.LParen:
                expr = ParseParenthesizedOrFunctionLiteral(expectation);
                break;
            case TokenKind.Super:
                expr = ParseSuperExpr();
                break;
            case TokenKind.If:
            {
                var start = Next().Span;
                Expect(TokenKind.LParen, "(");
                var condition = ParseExpr(")");
                Expect(TokenKind.RParen, ")");
                var then = ParseExpr("else");
                Expect(TokenKind.Else, "else");
                var otherwise = ParseExpr(expectation);
                expr = new IfExpr(condition, then, otherwise, Through(start, otherwise.Span));
                break;
            }
            case TokenKind.Let:
            {
                var start = Next().Span;
                Expect(TokenKind.LParen, "(");
                var parameter = ParseParameter();
                Expect(TokenKind.Assign, "=");
                var binding = ParseExpr(")");
                Expect(TokenKind.RParen, ")");
                var body = ParseExpr();
                expr = new LetExpr(parameter, binding, body, Through(start, body.Span));
                break;
            }
            case TokenKind.True:
                expr = new BoolLiteralExpr(true, Next().Span);
                break;
            case TokenKind.False:
                expr = new BoolLiteralExpr(false, Next().Span);
                break;
            case TokenKind.Int:
            case TokenKind.Hex:
            case TokenKind.Bin:
            case TokenKind.Oct:
            {
                var token = Next();
                expr = new IntLiteralExpr(TextOf(token), token.Span);
                break;
            }
            case TokenKind.Float:
            {
                var token = Next();
                expr = new FloatLiteralExpr(TextOf(token), token.Span);
                break;
            }
            case TokenKind.StringStart:
                expr = ParseSingleLineString();
                break;
            case TokenKind.StringMultiStart:
                expr = ParseMultiLineString();
                break;
            case TokenKind.Identifier:
            {
                var name = ParseIdentifier();
                var typeArguments = ParseOptionalTypeArguments();
                if (Lookahead == TokenKind.LParen && OnSameLine)
                {
                    var arguments = ParseArgumentList();
                    expr = new UnqualifiedAccessExpr(name, typeArguments, arguments, Through(name.Span, arguments.Span));
                }
                else if (typeArguments != null)
                {
                    throw UnexpectedToken("(");
                }
                else
                {
                    expr = new UnqualifiedAccessExpr(name, null, null, name.Span);
                }

                break;
            }
            case TokenKind.Eof:
                throw new PklParseException("Unexpected end of file", PreviousEnd());
            default:
                if (expectation != null)
                {
                    throw UnexpectedToken(expectation);
                }

                throw Error($"Unexpected token `{LookaheadText}`, expected an expression");
        }

        return ParseExprRest(expr);
    }

    private Expr ParseSuperExpr()
    {
        var start = Next().Span;
        if (Lookahead != TokenKind.Dot)
        {
            Expect(TokenKind.LBrack, "[");
            var index = ParseExpr("]");
            var close = Expect(TokenKind.RBrack, "]").Span;
            return new SuperSubscriptExpr(index, Through(start, close));
        }

        Next();
        var name = ParseIdentifier();
        var typeArguments = ParseOptionalTypeArguments();
        if (Lookahead == TokenKind.LParen && OnSameLine)
        {
            var arguments = ParseArgumentList();
            return new SuperAccessExpr(name, typeArguments, arguments, Through(start, arguments.Span));
        }

        if (typeArguments != null)
        {
            throw UnexpectedToken("(");
        }

        return new SuperAccessExpr(name, null, null, Through(start, name.Span));
    }

    /// <summary>Handles postfix constructs: `!!`, amendments, member access and subscripts.</summary>
    private Expr ParseExprRest(Expr expr)
    {
        while (true)
        {
            if (Lookahead == TokenKind.NonNull)
            {
                var end = Next().Span;
                expr = new NonNullExpr(expr, Through(expr.Span, end));
            }
            else if (Lookahead == TokenKind.LBrace)
            {
                if (expr is not (ParenthesizedExpr or AmendsExpr or NewExpr))
                {
                    var text = _source.Substring(expr.Span.Start, expr.Span.Length);
                    throw Error(
                        $"Unexpected `{{`. If you meant to amend `{text}`, wrap it in parentheses: `({text}) {{ ... }}`");
                }

                var body = ParseObjectBody();
                expr = new AmendsExpr(expr, body, Through(expr.Span, body.Span));
            }
            else if (Lookahead is TokenKind.Dot or TokenKind.QDot)
            {
                var isNullable = Next().Kind == TokenKind.QDot;
                expr = ParseMemberAccess(expr, isNullable);
            }
            else if (Lookahead == TokenKind.LBrack && OnSameLine)
            {
                Next();
                var index = ParseExpr("]");
                var end = Expect(TokenKind.RBrack, "]").Span;
                expr = new SubscriptExpr(expr, index, Through(expr.Span, end));
            }
            else
            {
                return expr;
            }
        }
    }

    /// <summary>After seeing `(`: either a parenthesized expression or a function literal.</summary>
    private Expr ParseParenthesizedOrFunctionLiteral(string? expectation)
    {
        var start = Next().Span;
        switch (Lookahead)
        {
            case TokenKind.Underscore:
                return ParseFunctionLiteral(start);
            case TokenKind.Identifier:
                return ParseFunctionLiteralOrParenthesized(start);
            case TokenKind.RParen:
            {
                var close = Next().Span;
                var parameters = new ParameterList(Array.Empty<Parameter>(), Through(start, close));
                Expect(TokenKind.Arrow, "->");
                var body = ParseExpr(expectation);
                return new FunctionLiteralExpr(parameters, body, Through(start, body.Span));
            }
            default:
            {
                var inner = ParseExpr(")");
                var end = Expect(TokenKind.RParen, ")").Span;
                return new ParenthesizedExpr(inner, Through(start, end));
            }
        }
    }

    private Expr ParseFunctionLiteralOrParenthesized(TextSpan start)
    {
        var identifier = ParseIdentifier();
        switch (Lookahead)
        {
            case TokenKind.Comma:
            {
                Next();
                var parameters = new List<Parameter> { new TypedIdentifier(identifier, null, identifier.Span) };
                parameters.AddRange(ParseListOfParameter(TokenKind.Comma, TokenKind.RParen));
                var close = Expect(TokenKind.RParen, ",", ")").Span;
                var list = new ParameterList(parameters, Through(start, close));
                Expect(TokenKind.Arrow, "->");
                var body = ParseExpr();
                return new FunctionLiteralExpr(list, body, Through(start, body.Span));
            }
            case TokenKind.Colon:
            {
                var annotation = ParseTypeAnnotation();
                var parameters = new List<Parameter>
                {
                    new TypedIdentifier(identifier, annotation, Through(identifier.Span, annotation.Span)),
                };
                if (Lookahead == TokenKind.Comma)
                {
                    Next();
                    parameters.AddRange(ParseListOfParameter(TokenKind.Comma, TokenKind.RParen));
                }

                var close = Expect(TokenKind.RParen, ",", ")").Span;
                var list = new ParameterList(parameters, Through(start, close));
                Expect(TokenKind.Arrow, "->");
                var body = ParseExpr(")");
                return new FunctionLiteralExpr(list, body, Through(start, body.Span));
            }
            case TokenKind.RParen:
            {
                // `(x)` is a parenthesized expression unless an arrow follows
                var close = Next().Span;
                if (Lookahead == TokenKind.Arrow)
                {
                    Next();
                    var body = ParseExpr();
                    var list = new ParameterList(
                        new Parameter[] { new TypedIdentifier(identifier, null, identifier.Span) },
                        Through(start, close));
                    return new FunctionLiteralExpr(list, body, Through(start, body.Span));
                }

                var access = new UnqualifiedAccessExpr(identifier, null, null, identifier.Span);
                return new ParenthesizedExpr(access, Through(start, close));
            }
            default:
            {
                // an ordinary expression that happens to start with an identifier
                Backtrack();
                var inner = ParseExpr(")");
                var end = Expect(TokenKind.RParen, ")").Span;
                return new ParenthesizedExpr(inner, Through(start, end));
            }
        }
    }

    private FunctionLiteralExpr ParseFunctionLiteral(TextSpan start)
    {
        // the opening paren is already consumed
        var parameters = ParseListOfParameter(TokenKind.Comma, TokenKind.RParen);
        var close = Expect(TokenKind.RParen, ",", ")").Span;
        var list = new ParameterList(parameters, Through(start, close));
        Expect(TokenKind.Arrow, "->");
        var body = ParseExpr();
        return new FunctionLiteralExpr(list, body, Through(start, body.Span));
    }

    #endregion

    #region Strings

    private StringLiteralExpr ParseSingleLineString()
    {
        var start = Next();
        var parts = new List<StringPart>();
        var builder = new StringBuilder();
        var chunkStart = LookaheadSpan;
        var chunkEnd = LookaheadSpan;

        while (Lookahead != TokenKind.StringEnd)
        {
            switch (Lookahead)
            {
                case TokenKind.StringPart:
                {
                    var token = Next();
                    chunkEnd = token.Span;
                    builder.Append(TextOf(token));
                    break;
                }
                case TokenKind.StringEscapeNewline:
                case TokenKind.StringEscapeTab:
                case TokenKind.StringEscapeQuote:
                case TokenKind.StringEscapeBackslash:
                case TokenKind.StringEscapeReturn:
                case TokenKind.StringEscapeUnicode:
                {
                    var token = Next();
                    chunkEnd = token.Span;
                    builder.Append(GetEscapeText(token));
                    break;
                }
                case TokenKind.StringEscapeContinuation:
                    throw Error("Line continuation escape sequences are only allowed in multi-line strings");
                case TokenKind.InterpolationStart:
                {
                    var interpolationStart = Next().Span;
                    if (builder.Length > 0)
                    {
                        parts.Add(new StringChars(builder.ToString(), Through(chunkStart, chunkEnd)));
                        builder.Clear();
                    }

                    var expression = ParseExpr(")");
                    chunkEnd = Expect(TokenKind.RParen, ")").Span;
                    parts.Add(new StringInterpolation(expression, Through(interpolationStart, chunkEnd)));
                    chunkStart = LookaheadSpan;
                    break;
                }
                case TokenKind.Eof:
                    throw MissingStringDelimiter(start);
                default:
                    throw Error($"Unexpected token `{LookaheadText}` in string");
            }
        }

        if (builder.Length > 0)
        {
            parts.Add(new StringChars(builder.ToString(), Through(chunkStart, chunkEnd)));
        }

        var end = Next().Span;
        return new StringLiteralExpr(parts, false, Through(start.Span, end));
    }

    private readonly struct StringToken
    {
        public StringToken(Token? token, StringInterpolation? interpolation)
        {
            Token = token;
            Interpolation = interpolation;
        }

        public Token? Token { get; }
        public StringInterpolation? Interpolation { get; }
        public TextSpan Span => Token?.Span ?? Interpolation!.Span;
    }

    private StringLiteralExpr ParseMultiLineString()
    {
        var start = Next();
        var pieces = new List<StringToken>();
        while (Lookahead != TokenKind.StringEnd)
        {
            switch (Lookahead)
            {
                case TokenKind.StringPart:
                case TokenKind.StringNewline:
                case TokenKind.StringEscapeNewline:
                case TokenKind.StringEscapeTab:
                case TokenKind.StringEscapeQuote:
                case TokenKind.StringEscapeBackslash:
                case TokenKind.StringEscapeReturn:
                case TokenKind.StringEscapeUnicode:
                case TokenKind.StringEscapeContinuation:
                    pieces.Add(new StringToken(Next(), null));
                    break;
                case TokenKind.InterpolationStart:
                {
                    var interpolationStart = Next();
                    var expression = ParseExpr(")");
                    var end = Expect(TokenKind.RParen, ")").Span;
                    pieces.Add(new StringToken(null, new StringInterpolation(expression, Through(interpolationStart.Span, end))));
                    break;
                }
                case TokenKind.Eof:
                    throw MissingStringDelimiter(start);
                default:
                    throw Error($"Unexpected token `{LookaheadText}` in string");
            }
        }

        var close = Next().Span;
        var fullSpan = Through(start.Span, close);
        var parts = ValidateMultiLineString(pieces, fullSpan);
        return new StringLiteralExpr(parts, true, fullSpan);
    }

    private PklParseException MissingStringDelimiter(Token startToken)
    {
        var delimiter = new string(TextOf(startToken).Reverse().ToArray());
        return Error($"Missing closing delimiter `{delimiter}`");
    }

    private List<StringPart> ValidateMultiLineString(List<StringToken> pieces, TextSpan span)
    {
        var first = pieces.Count == 0 ? (StringToken?)null : pieces[0];
        if (first == null || first.Value.Token?.Kind != TokenKind.StringNewline)
        {
            throw new PklParseException(
                "String content must begin on a new line", first?.Span ?? span);
        }

        // only a newline
        if (pieces.Count == 1)
        {
            return new List<StringPart> { new StringChars(string.Empty, first.Value.Span) };
        }

        var indent = GetCommonIndent(pieces, span);
        return RenderMultiLineString(pieces, indent);
    }

    private string GetCommonIndent(List<StringToken> pieces, TextSpan span)
    {
        var last = pieces[pieces.Count - 1];
        if (last.Token == null)
        {
            throw new PklParseException("The closing string delimiter must begin on a new line", last.Span);
        }

        if (last.Token.Value.Kind == TokenKind.StringNewline)
        {
            return string.Empty;
        }

        var beforeLast = pieces[pieces.Count - 2];
        if (beforeLast.Token?.Kind == TokenKind.StringNewline && last.Token.Value.Kind == TokenKind.StringPart)
        {
            var text = TextOf(last.Token.Value);
            if (text.All(c => c is ' ' or '\t'))
            {
                return text;
            }
        }

        throw new PklParseException("The closing string delimiter must begin on a new line", span);
    }

    private List<StringPart> RenderMultiLineString(List<StringToken> pieces, string commonIndent)
    {
        var parts = new List<StringPart>();
        var builder = new StringBuilder();
        var lastToken = pieces[pieces.Count - 1].Token!.Value;
        // the last line only holds the closing delimiter's indentation
        var endOffset = lastToken.Kind == TokenKind.StringNewline ? 1 : 2;
        var isNewLine = true;
        TextSpan? chunkStart = null;
        TextSpan chunkEnd = default;

        void FlushChars()
        {
            if (builder.Length > 0)
            {
                parts.Add(new StringChars(builder.ToString(), Through(chunkStart!.Value, chunkEnd)));
                builder.Clear();
            }

            chunkStart = null;
        }

        for (var i = 1; i < pieces.Count - endOffset; i++)
        {
            var piece = pieces[i];
            if (piece.Interpolation != null)
            {
                FlushChars();
                parts.Add(piece.Interpolation);
                continue;
            }

            var token = piece.Token!.Value;
            chunkStart ??= token.Span;
            chunkEnd = token.Span;

            switch (token.Kind)
            {
                case TokenKind.StringNewline:
                    builder.Append('\n');
                    isNewLine = true;
                    break;
                case TokenKind.StringEscapeContinuation:
                    isNewLine = true;
                    break;
                case TokenKind.StringPart:
                {
                    var text = TextOf(token);
                    if (isNewLine)
                    {
                        if (text.StartsWith(commonIndent, StringComparison.Ordinal))
                        {
                            builder.Append(text, commonIndent.Length, text.Length - commonIndent.Length);
                        }
                        else
                        {
                            var actualIndent = text.TakeWhile(c => c is ' ' or '\t').Count();
                            throw new PklParseException(
                                "String indentation must match the indentation of the closing delimiter",
                                new TextSpan(token.Span.Start + actualIndent, token.Span.Length - actualIndent));
                        }
                    }
                    else
                    {
                        builder.Append(text);
                    }

                    isNewLine = false;
                    break;
                }
                default:
                    if (isNewLine && commonIndent.Length > 0)
                    {
                        throw new PklParseException(
                            "String indentation must match the indentation of the closing delimiter", token.Span);
                    }

                    builder.Append(GetEscapeText(token));
                    isNewLine = false;
                    break;
            }
        }

        FlushChars();
        return parts;
    }

    private string GetEscapeText(Token token) => token.Kind switch
    {
        TokenKind.StringEscapeNewline => "\n",
        TokenKind.StringEscapeQuote => "\"",
        TokenKind.StringEscapeBackslash => "\\",
        TokenKind.StringEscapeTab => "\t",
        TokenKind.StringEscapeReturn => "\r",
        TokenKind.StringEscapeContinuation => string.Empty,
        TokenKind.StringEscapeUnicode => ParseUnicodeEscape(token),
        _ => throw new InvalidOperationException($"{token.Kind} is not an escape sequence"),
    };

    private string ParseUnicodeEscape(Token token)
    {
        var text = TextOf(token);
        var braceIndex = text.IndexOf('{', 2);
        var digits = text.Substring(braceIndex + 1, text.Length - braceIndex - 2);
        try
        {
            var codePoint = Convert.ToInt32(digits, 16);
            // Pkl accepts lone surrogates, which ConvertFromUtf32 rejects
            return codePoint is >= 0xD800 and <= 0xDFFF
                ? ((char)codePoint).ToString()
                : char.ConvertFromUtf32(codePoint);
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
        {
            throw new PklParseException(
                $"Invalid unicode escape sequence `{text}`", token.Span);
        }
    }

    private StringConstant ParseStringConstant()
    {
        var start = LookaheadSpan;
        var startToken = Expect(TokenKind.StringStart, "\"");
        var builder = new StringBuilder();
        while (Lookahead != TokenKind.StringEnd)
        {
            switch (Lookahead)
            {
                case TokenKind.StringPart:
                    builder.Append(TextOf(Next()));
                    break;
                case TokenKind.StringEscapeNewline:
                case TokenKind.StringEscapeTab:
                case TokenKind.StringEscapeQuote:
                case TokenKind.StringEscapeBackslash:
                case TokenKind.StringEscapeReturn:
                case TokenKind.StringEscapeUnicode:
                    builder.Append(GetEscapeText(Next()));
                    break;
                case TokenKind.Eof:
                    throw MissingStringDelimiter(startToken);
                case TokenKind.InterpolationStart:
                    throw Error("String interpolation is not allowed here");
                default:
                    throw Error($"Unexpected token `{LookaheadText}` in string");
            }
        }

        var end = Next().Span;
        return new StringConstant(builder.ToString(), Through(start, end));
    }

    #endregion

    #region Types

    private TypeNode ParseType(string? expectation = null) => Nested(() => ParseTypeCore(expectation));

    private TypeNode ParseTypeCore(string? expectation)
    {
        var defaultIndex = -1;
        TextSpan? start = null;
        if (Lookahead == TokenKind.Star)
        {
            defaultIndex = 0;
            start = Next().Span;
        }

        var first = ParseTypeAtom(expectation);
        start ??= first.Span;

        if (Lookahead != TokenKind.Union)
        {
            if (defaultIndex == 0)
            {
                throw new PklParseException(
                    "A default type marker `*` is only allowed in union types", Through(start.Value, first.Span));
            }

            return first;
        }

        var members = new List<TypeNode> { first };
        var end = first.Span;
        var index = 1;
        while (Lookahead == TokenKind.Union)
        {
            Next();
            if (Lookahead == TokenKind.Star)
            {
                if (defaultIndex != -1)
                {
                    throw Error("A union type can only have one default type marker `*`");
                }

                defaultIndex = index;
                Next();
            }

            var member = ParseTypeAtom(expectation);
            members.Add(member);
            end = member.Span;
            index++;
        }

        return new UnionType(members, defaultIndex, Through(start.Value, end));
    }

    private TypeNode ParseTypeAtom(string? expectation)
    {
        TypeNode type;
        switch (Lookahead)
        {
            case TokenKind.Unknown:
                type = new UnknownType(Next().Span);
                break;
            case TokenKind.Nothing:
                type = new NothingType(Next().Span);
                break;
            case TokenKind.Module:
                type = new ModuleType(Next().Span);
                break;
            case TokenKind.This:
                type = new ThisType(Next().Span);
                break;
            case TokenKind.LParen:
            {
                var open = Next().Span;
                var children = new List<TypeNode>();
                TextSpan end;
                if (Lookahead == TokenKind.RParen)
                {
                    end = Next().Span;
                }
                else
                {
                    children.AddRange(ParseListOf(TokenKind.Comma, TokenKind.RParen, () => ParseType(")")));
                    end = Expect(TokenKind.RParen, ",", ")").Span;
                }

                if (Lookahead == TokenKind.Arrow || children.Count > 1)
                {
                    Expect(TokenKind.Arrow, "->");
                    var returnType = ParseType(expectation);
                    return new FunctionType(children, returnType, Through(open, returnType.Span));
                }

                if (children.Count == 0)
                {
                    throw new PklParseException("Unexpected token `)`, expected a type", end);
                }

                type = new ParenthesizedType(children[0], Through(open, end));
                break;
            }
            case TokenKind.Identifier:
            {
                var start = LookaheadSpan;
                var name = ParseQualifiedIdentifier();
                var end = name.Span;
                TypeArgumentList? typeArguments = null;
                if (Lookahead == TokenKind.Lt)
                {
                    typeArguments = ParseTypeArgumentList();
                    end = typeArguments.Span;
                }

                type = new DeclaredType(name, typeArguments, Through(start, end));
                break;
            }
            case TokenKind.StringStart:
            {
                var value = ParseStringConstant();
                type = new StringLiteralType(value, value.Span);
                break;
            }
            default:
                if (expectation != null)
                {
                    throw Error($"Unexpected token `{LookaheadText}`, expected a type or `{expectation}`");
                }

                throw Error($"Unexpected token `{LookaheadText}`, expected a type");
        }

        return ParseTypeEnd(type);
    }

    private TypeNode ParseTypeEnd(TypeNode type)
    {
        while (true)
        {
            if (Lookahead == TokenKind.Question)
            {
                var end = Next().Span;
                type = new NullableType(type, Through(type.Span, end));
            }
            else if (Lookahead == TokenKind.LParen && OnSameLine)
            {
                // constraints must start on the same line as the type
                Next();
                var constraints = ParseListOf(TokenKind.Comma, TokenKind.RParen, () => ParseExpr(")"));
                var end = Expect(TokenKind.RParen, ",", ")").Span;
                type = new ConstrainedType(type, constraints, Through(type.Span, end));
            }
            else
            {
                return type;
            }
        }
    }

    private TypeAnnotation ParseTypeAnnotation()
    {
        var start = Expect(TokenKind.Colon, ":").Span;
        var type = ParseType();
        return new TypeAnnotation(type, Through(start, type.Span));
    }

    private TypeParameterList ParseTypeParameterList(bool allowVariance)
    {
        var start = Expect(TokenKind.Lt, "<").Span;
        var parameters = ParseListOf(TokenKind.Comma, TokenKind.Gt, () => ParseTypeParameter(allowVariance));
        var end = Expect(TokenKind.Gt, ",", ">").Span;
        return new TypeParameterList(parameters, Through(start, end));
    }

    private TypeParameter ParseTypeParameter(bool allowVariance)
    {
        var variance = Variance.None;
        var start = LookaheadSpan;
        if (Lookahead is TokenKind.In or TokenKind.Out)
        {
            if (!allowVariance)
            {
                throw Error("Type parameter variance is not allowed here");
            }

            variance = Next().Kind == TokenKind.In ? Variance.In : Variance.Out;
        }

        var name = ParseIdentifier();
        return new TypeParameter(variance, name, Through(start, name.Span));
    }

    private TypeArgumentList ParseTypeArgumentList()
    {
        var start = Expect(TokenKind.Lt, "<").Span;
        var arguments = ParseListOf(TokenKind.Comma, TokenKind.Gt, () => ParseType());
        var end = Expect(TokenKind.Gt, ",", ">").Span;
        return new TypeArgumentList(arguments, Through(start, end));
    }

    #endregion

    #region Parameters and identifiers

    private ParameterList ParseParameterList()
    {
        var start = Expect(TokenKind.LParen, "(").Span;
        var parameters = new List<Parameter>();
        TextSpan end;
        if (Lookahead == TokenKind.RParen)
        {
            end = Next().Span;
        }
        else
        {
            parameters = ParseListOfParameter(TokenKind.Comma, TokenKind.RParen);
            end = Expect(TokenKind.RParen, ",", ")").Span;
        }

        return new ParameterList(parameters, Through(start, end));
    }

    private ArgumentList ParseArgumentList()
    {
        var start = Expect(TokenKind.LParen, "(").Span;
        if (Lookahead == TokenKind.RParen)
        {
            return new ArgumentList(Array.Empty<Expr>(), Through(start, Next().Span));
        }

        var arguments = ParseListOf(TokenKind.Comma, TokenKind.RParen, () => ParseExpr());
        var end = Expect(TokenKind.RParen, ",", ")").Span;
        return new ArgumentList(arguments, Through(start, end));
    }

    private Parameter ParseParameter()
    {
        if (Lookahead == TokenKind.Underscore)
        {
            return new UnderscoreParameter(Next().Span);
        }

        var name = ParseIdentifier();
        TypeAnnotation? annotation = null;
        var end = name.Span;
        if (Lookahead == TokenKind.Colon)
        {
            annotation = ParseTypeAnnotation();
            end = annotation.Span;
        }

        return new TypedIdentifier(name, annotation, Through(name.Span, end));
    }

    private List<Parameter> ParseListOfParameter(TokenKind separator, TokenKind terminator)
    {
        var result = new List<Parameter>();
        if (Lookahead == terminator)
        {
            return result;
        }

        result.Add(ParseParameter());
        while (Lookahead == separator)
        {
            Next();
            if (Lookahead == terminator)
            {
                break;
            }

            result.Add(ParseParameter());
        }

        return result;
    }

    private List<T> ParseListOf<T>(TokenKind separator, TokenKind terminator, Func<T> parse)
    {
        var result = new List<T> { parse() };
        while (Lookahead == separator)
        {
            Next();
            if (Lookahead == terminator)
            {
                break;
            }

            result.Add(parse());
        }

        return result;
    }

    private QualifiedIdentifier ParseQualifiedIdentifier()
    {
        var parts = new List<Identifier> { ParseIdentifier() };
        while (Lookahead == TokenKind.Dot)
        {
            Next();
            parts.Add(ParseIdentifier());
        }

        return new QualifiedIdentifier(parts, Through(parts[0].Span, parts[parts.Count - 1].Span));
    }

    private Identifier ParseIdentifier()
    {
        if (Lookahead != TokenKind.Identifier)
        {
            if (Lookahead.IsKeyword())
            {
                throw Error($"Keyword `{Lookahead.Text()}` is not allowed here");
            }

            throw UnexpectedToken("identifier");
        }

        var token = Next();
        return new Identifier(TextOf(token), token.Span);
    }

    #endregion
}
