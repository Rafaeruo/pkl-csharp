using PklGenerator.Parsing;
using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Tests.Parsing;

public class ParserExpressionTests
{
    private static Expr Parse(string source) => Parser.ParseExpression(source);

    [Fact]
    public void Literals()
    {
        Assert.IsType<NullLiteralExpr>(Parse("null"));
        Assert.True(Assert.IsType<BoolLiteralExpr>(Parse("true")).Value);
        Assert.Equal("0xFF", Assert.IsType<IntLiteralExpr>(Parse("0xFF")).Text);
        Assert.Equal("1.5e3", Assert.IsType<FloatLiteralExpr>(Parse("1.5e3")).Text);
    }

    [Fact]
    public void MultiplicationBindsTighterThanAddition()
    {
        var expr = Assert.IsType<BinaryOperatorExpr>(Parse("1 + 2 * 3"));
        Assert.Equal(BinaryOperator.Plus, expr.Operator);
        Assert.Equal(BinaryOperator.Multiply, Assert.IsType<BinaryOperatorExpr>(expr.Right).Operator);
    }

    [Fact]
    public void SubtractionIsLeftAssociative()
    {
        var expr = Assert.IsType<BinaryOperatorExpr>(Parse("10 - 3 - 2"));
        Assert.IsType<BinaryOperatorExpr>(expr.Left);
        Assert.IsType<IntLiteralExpr>(expr.Right);
    }

    [Fact]
    public void PowerAndNullCoalesceAreRightAssociative()
    {
        var pow = Assert.IsType<BinaryOperatorExpr>(Parse("2 ** 3 ** 2"));
        Assert.IsType<BinaryOperatorExpr>(pow.Right);

        var coalesce = Assert.IsType<BinaryOperatorExpr>(Parse("a ?? b ?? c"));
        Assert.IsType<BinaryOperatorExpr>(coalesce.Right);
    }

    [Fact]
    public void UnaryMinusBindsTighterThanBinaryOperators()
    {
        var expr = Assert.IsType<BinaryOperatorExpr>(Parse("-2 ** 2"));
        Assert.IsType<UnaryMinusExpr>(expr.Left);
    }

    [Fact]
    public void LogicalNotAndNonNull()
    {
        Assert.IsType<LogicalNotExpr>(Parse("!a"));
        Assert.IsType<NonNullExpr>(Parse("a!!"));
    }

    [Fact]
    public void MemberAccessChains()
    {
        var expr = Assert.IsType<QualifiedAccessExpr>(Parse("a.b?.c(1, 2)"));
        Assert.Equal("c", expr.Name.Name);
        Assert.True(expr.IsNullable);
        Assert.Equal(2, expr.Arguments!.Arguments.Count);
        var inner = Assert.IsType<QualifiedAccessExpr>(expr.Receiver);
        Assert.False(inner.IsNullable);
    }

    [Fact]
    public void MethodCallWithTypeArguments()
    {
        var expr = Assert.IsType<UnqualifiedAccessExpr>(Parse("foo::<String>(1)"));
        Assert.Single(expr.TypeArguments!.Arguments);
    }

    [Fact]
    public void SubscriptMustBeOnTheSameLine()
    {
        Assert.IsType<SubscriptExpr>(Parse("a[0]"));
        // on a new line `[` can't be a subscript, so it starts something else and the input is incomplete
        Assert.Throws<PklParseException>(() => Parse("a\n[0]"));
    }

    [Fact]
    public void IfAndLet()
    {
        var conditional = Assert.IsType<IfExpr>(Parse("if (a) 1 else 2"));
        Assert.IsType<IntLiteralExpr>(conditional.Else);

        var let = Assert.IsType<LetExpr>(Parse("let (x = 1) x + 1"));
        Assert.IsType<BinaryOperatorExpr>(let.Body);
    }

    [Fact]
    public void TypeTestsAndCasts()
    {
        Assert.IsType<TypeCheckExpr>(Parse("a is String"));
        Assert.IsType<TypeCastExpr>(Parse("a as Int"));
    }

    [Fact]
    public void FunctionLiteralsAndParenthesizedExpressions()
    {
        Assert.IsType<ParenthesizedExpr>(Parse("(a)"));
        Assert.IsType<ParenthesizedExpr>(Parse("(a + b)"));
        Assert.Single(Assert.IsType<FunctionLiteralExpr>(Parse("(x) -> x + 1")).Parameters.Parameters);
        Assert.Equal(2, Assert.IsType<FunctionLiteralExpr>(Parse("(x, y: Int) -> x")).Parameters.Parameters.Count);
        Assert.Empty(Assert.IsType<FunctionLiteralExpr>(Parse("() -> 1")).Parameters.Parameters);
        Assert.IsType<UnderscoreParameter>(
            Assert.IsType<FunctionLiteralExpr>(Parse("(_) -> 1")).Parameters.Parameters[0]);
    }

    [Fact]
    public void NewExpressions()
    {
        var typed = Assert.IsType<NewExpr>(Parse("new Listing<Int> { 1 2 3 }"));
        Assert.Equal(3, typed.Body.Members.Count);
        Assert.NotNull(typed.Type);

        var inferred = Assert.IsType<NewExpr>(Parse("new { a = 1 }"));
        Assert.Null(inferred.Type);
    }

    [Fact]
    public void AmendingRequiresParenthesesOrNew()
    {
        Assert.IsType<AmendsExpr>(Parse("(base) { a = 1 }"));
        Assert.IsType<AmendsExpr>(Parse("new Foo { a = 1 } { b = 2 }"));
        Assert.Throws<PklParseException>(() => Parse("base { a = 1 }"));
    }

    [Fact]
    public void ImportAndReadExpressions()
    {
        Assert.False(Assert.IsType<ImportExpr>(Parse("import(\"a.pkl\")")).IsGlob);
        Assert.True(Assert.IsType<ImportExpr>(Parse("import*(\"*.pkl\")")).IsGlob);
        Assert.Equal(ReadKind.ReadOrNull, Assert.IsType<ReadExpr>(Parse("read?(\"env:X\")")).Kind);
        Assert.Equal(ReadKind.ReadGlob, Assert.IsType<ReadExpr>(Parse("read*(\"env:*\")")).Kind);
    }

    [Fact]
    public void ThrowTraceSuperThisOuterModule()
    {
        Assert.IsType<ThrowExpr>(Parse("throw(\"no\")"));
        Assert.IsType<TraceExpr>(Parse("trace(1)"));
        Assert.IsType<SuperAccessExpr>(Parse("super.foo"));
        Assert.IsType<SuperSubscriptExpr>(Parse("super[1]"));
        Assert.IsType<ThisExpr>(Parse("this"));
        Assert.IsType<OuterExpr>(Parse("outer"));
        Assert.IsType<ModuleExpr>(Parse("module"));
    }

    [Fact]
    public void ObjectBodyMembers()
    {
        var body = Assert.IsType<NewExpr>(Parse("""
            new Dynamic {
              a = 1
              b: Int = 2
              local c = 3
              nested { d = 4 }
              ["key"] = "v"
              [[startsWith("x")]] { e = 5 }
              ...other
              ...?maybe
              when (flag) { f = 6 } else { g = 7 }
              for (k, v in map) { [k] = v }
              function fn(x) = x
              "element"
            }
            """)).Body;

        Assert.Collection(
            body.Members,
            m => Assert.IsType<ObjectProperty>(m),
            m => Assert.NotNull(Assert.IsType<ObjectProperty>(m).TypeAnnotation),
            m => Assert.Equal(ModifierKind.Local, Assert.IsType<ObjectProperty>(m).Modifiers[0].Kind),
            m => Assert.Single(Assert.IsType<ObjectProperty>(m).Bodies),
            m => Assert.IsType<ObjectEntry>(m),
            m => Assert.IsType<MemberPredicate>(m),
            m => Assert.False(Assert.IsType<ObjectSpread>(m).IsNullable),
            m => Assert.True(Assert.IsType<ObjectSpread>(m).IsNullable),
            m => Assert.NotNull(Assert.IsType<WhenGenerator>(m).ElseBody),
            m => Assert.NotNull(Assert.IsType<ForGenerator>(m).Second),
            m => Assert.IsType<ObjectMethod>(m),
            m => Assert.IsType<ObjectElement>(m));
    }

    [Fact]
    public void ObjectBodyParameters()
    {
        var one = Assert.IsType<NewExpr>(Parse("new Listing { x -> x }")).Body;
        Assert.Single(one.Parameters);

        var typed = Assert.IsType<NewExpr>(Parse("new Mapping { k: String, v -> v }")).Body;
        Assert.Equal(2, typed.Parameters.Count);

        var underscore = Assert.IsType<NewExpr>(Parse("new Mapping { _, v -> v }")).Body;
        Assert.IsType<UnderscoreParameter>(underscore.Parameters[0]);
    }

    [Fact]
    public void ObjectBodyElementsThatLookLikeProperties()
    {
        // `x` alone is an element, not a property
        var body = Assert.IsType<NewExpr>(Parse("new Listing { x\n y }")).Body;
        Assert.Equal(2, body.Members.Count);
        Assert.All(body.Members, m => Assert.IsType<ObjectElement>(m));
    }

    [Fact]
    public void SimpleStrings()
    {
        var str = Assert.IsType<StringLiteralExpr>(Parse("\"a\\tb\\n\\\"\\\\\""));
        Assert.Equal("a\tb\n\"\\", str.ConstantValue);
        Assert.Equal(string.Empty, Assert.IsType<StringLiteralExpr>(Parse("\"\"")).ConstantValue);
    }

    [Fact]
    public void UnicodeEscapes()
    {
        Assert.Equal("A", ((StringLiteralExpr)Parse("\"\\u{41}\"")).ConstantValue);
        Assert.Equal("\U0001F600", ((StringLiteralExpr)Parse("\"\\u{1F600}\"")).ConstantValue);
        Assert.Throws<PklParseException>(() => Parse("\"\\u{110000}\""));
    }

    [Fact]
    public void InterpolatedStrings()
    {
        var str = Assert.IsType<StringLiteralExpr>(Parse("\"a\\(x)b\\(f(1))\""));
        Assert.Null(str.ConstantValue);
        Assert.Collection(
            str.Parts,
            p => Assert.Equal("a", Assert.IsType<StringChars>(p).Value),
            p => Assert.IsType<UnqualifiedAccessExpr>(Assert.IsType<StringInterpolation>(p).Expression),
            p => Assert.Equal("b", Assert.IsType<StringChars>(p).Value),
            p => Assert.IsType<StringInterpolation>(p));
    }

    [Fact]
    public void RawStrings()
    {
        Assert.Equal("a\\nb", ((StringLiteralExpr)Parse("#\"a\\nb\"#")).ConstantValue);
        Assert.Equal("a\nb", ((StringLiteralExpr)Parse("#\"a\\#nb\"#")).ConstantValue);
        Assert.Equal("say \"hi\"", ((StringLiteralExpr)Parse("#\"say \"hi\"\"#")).ConstantValue);
    }

    [Fact]
    public void MultiLineStringStripsCommonIndent()
    {
        var str = Assert.IsType<StringLiteralExpr>(Parse("\"\"\"\n    one\n      two\n    \"\"\""));
        Assert.True(str.IsMultiLine);
        Assert.Equal("one\n  two", str.ConstantValue);
    }

    [Fact]
    public void MultiLineStringWithInterpolationAndContinuation()
    {
        var str = Assert.IsType<StringLiteralExpr>(Parse("\"\"\"\n  a \\(x)\n  b \\\n  c\n  \"\"\""));
        Assert.Equal(3, str.Parts.Count);
        Assert.Equal("a ", ((StringChars)str.Parts[0]).Value);
        Assert.Equal("\nb c", ((StringChars)str.Parts[2]).Value);
    }

    [Theory]
    [InlineData("\"\"\"content on first line\n\"\"\"")]
    [InlineData("\"\"\"\n  a\n b\n  \"\"\"")]
    [InlineData("\"\"\"\n  a  \"\"\"")]
    public void InvalidMultiLineStrings(string source)
    {
        Assert.Throws<PklParseException>(() => Parse(source));
    }

    [Fact]
    public void ConstantStringsRejectInterpolation()
    {
        Assert.Throws<PklParseException>(() => Parser.ParseModule("import \"a\\(b).pkl\""));
    }

    [Fact]
    public void TypeSyntax()
    {
        static TypeNode TypeOf(string type) =>
            Assert.IsType<NewExpr>(Parser.ParseExpression($"new {type} {{}}")).Type!;

        Assert.IsType<NullableType>(TypeOf("String?"));
        Assert.IsType<UnionType>(TypeOf("Int|String"));
        Assert.Equal(1, Assert.IsType<UnionType>(TypeOf("Int|*String")).DefaultIndex);
        Assert.IsType<ConstrainedType>(TypeOf("Int(this > 0, this < 10)"));
        Assert.IsType<DeclaredType>(TypeOf("Map<String, List<Int>>"));
        Assert.IsType<FunctionType>(TypeOf("(Int, Int) -> Int"));
        Assert.IsType<ParenthesizedType>(TypeOf("(Int)"));
        Assert.IsType<StringLiteralType>(TypeOf("\"a\""));
        Assert.IsType<UnknownType>(TypeOf("unknown"));
        Assert.IsType<NothingType>(TypeOf("nothing"));
        Assert.IsType<ModuleType>(TypeOf("module"));
        var qualified = Assert.IsType<DeclaredType>(TypeOf("imported.Foo"));
        Assert.Equal(2, qualified.Name.Parts.Count);
    }

    [Fact]
    public void NestedGenericsWithClosingAngles()
    {
        var type = Assert.IsType<DeclaredType>(Assert.IsType<NewExpr>(Parse("new Map<String, Listing<Int>> {}")).Type);
        Assert.Equal(2, type.TypeArguments!.Arguments.Count);
    }
}
