using PklGenerator.Parsing;
using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Tests.Parsing;

public class ParserDeclarationTests
{
    private static PklModule Parse(string source) => Parser.ParseModule(source);

    [Fact]
    public void EmptyModule()
    {
        var module = Parse("");
        Assert.Null(module.ModuleDeclaration);
        Assert.Empty(module.Imports);
        Assert.Empty(module.Members);
    }

    [Fact]
    public void ModuleOfOnlyComments()
    {
        var module = Parse("// nothing here\n/* or here */\n");
        Assert.Empty(module.Members);
    }

    [Fact]
    public void ModuleHeader()
    {
        var module = Parse("""
                           /// A config
                           @Deprecated
                           open module my.pkg.Config
                           extends "base.pkl"
                           """);

        var decl = Assert.IsType<ModuleDecl>(module.ModuleDeclaration);
        Assert.Equal("my.pkg.Config", decl.Name!.ToString());
        Assert.Equal("A config", decl.Header.DocComment!.Text);
        Assert.Single(decl.Header.Annotations);
        Assert.Equal(ModifierKind.Open, Assert.Single(decl.Header.Modifiers).Kind);
        Assert.Equal(ExtendsOrAmendsKind.Extends, decl.ExtendsOrAmends!.Kind);
        Assert.Equal("base.pkl", decl.ExtendsOrAmends.Url.Value);
    }

    [Fact]
    public void AmendsOnlyModule()
    {
        var module = Parse("amends \"./Schema.pkl\"\n\nhost = \"x\"");
        Assert.Equal(ExtendsOrAmendsKind.Amends, module.ModuleDeclaration!.ExtendsOrAmends!.Kind);
        Assert.Null(module.ModuleDeclaration.Name);
        Assert.Single(module.Members);
    }

    [Fact]
    public void Imports()
    {
        var module = Parse("import \"a.pkl\"\nimport* \"b/*.pkl\" as bs\nimport \"c.pkl\" as c\n\nx = 1");
        Assert.Equal(3, module.Imports.Count);
        Assert.False(module.Imports[0].IsGlob);
        Assert.True(module.Imports[1].IsGlob);
        Assert.Equal("bs", module.Imports[1].Alias!.Name);
        Assert.Equal("c.pkl", module.Imports[2].Url.Value);
    }

    [Fact]
    public void ImportsMustPrecedeMembers()
    {
        Assert.Throws<PklParseException>(() => Parse("x = 1\nimport \"a.pkl\""));
    }

    [Fact]
    public void PropertiesWithTypesAndValues()
    {
        var module = Parse("""
                           host: String = "localhost"
                           port: Int
                           name = "x"
                           """);

        var properties = module.Members.Cast<ClassPropertyDecl>().ToList();
        Assert.Equal(3, properties.Count);
        Assert.NotNull(properties[0].TypeAnnotation);
        Assert.NotNull(properties[0].Value);
        Assert.Null(properties[1].Value);
        Assert.Null(properties[2].TypeAnnotation);
    }

    [Fact]
    public void PropertyWithoutTypeOrValueIsAnError()
    {
        Assert.Throws<PklParseException>(() => Parse("foo"));
    }

    [Fact]
    public void PropertyAmendmentBodies()
    {
        var module = Parse("nested { a = 1 } { b = 2 }");
        var property = Assert.IsType<ClassPropertyDecl>(Assert.Single(module.Members));
        Assert.Equal(2, property.Bodies.Count);
    }

    [Fact]
    public void ClassWithSuperclassAndMembers()
    {
        var module = Parse("""
                           /// Doc
                           abstract class Dog extends Animal {
                             name: String
                             hidden secret: String = "s"
                             function bark(times: Int): String = "woof"
                           }
                           """);

        var dog = Assert.IsType<ClassDecl>(Assert.Single(module.Members));
        Assert.Equal("Dog", dog.Name.Name);
        Assert.True(dog.Header.HasModifier(ModifierKind.Abstract));
        Assert.Equal("Animal", ((DeclaredType)dog.Superclass!).Name.ToString());
        Assert.Equal(3, dog.Body!.Members.Count);
        Assert.IsType<ClassPropertyDecl>(dog.Body.Members[0]);
        Assert.True(dog.Body.Members[1].Header.HasModifier(ModifierKind.Hidden));
        Assert.IsType<ClassMethodDecl>(dog.Body.Members[2]);
    }

    [Fact]
    public void ClassWithoutBody()
    {
        var module = Parse("class Empty");
        Assert.Null(Assert.IsType<ClassDecl>(Assert.Single(module.Members)).Body);
    }

    [Fact]
    public void TypeAliases()
    {
        var module = Parse("typealias Port = UInt16(this > 0)\ntypealias Mode = \"a\"|\"b\"\ntypealias Box<T> = List<T>");
        Assert.Equal(3, module.Members.Count);
        Assert.IsType<ConstrainedType>(((TypeAliasDecl)module.Members[0]).Type);
        Assert.IsType<UnionType>(((TypeAliasDecl)module.Members[1]).Type);
        Assert.NotNull(((TypeAliasDecl)module.Members[2]).TypeParameters);
    }

    [Fact]
    public void MethodsAtModuleLevel()
    {
        var module = Parse("function twice(x: Int): Int = x * 2\nlocal function noop() = null");
        Assert.Equal(2, module.Members.Count);
        Assert.True(module.Members[1].Header.HasModifier(ModifierKind.Local));
    }

    [Fact]
    public void DocCommentLinesAreJoined()
    {
        var module = Parse("/// first\n/// second\nx = 1");
        Assert.Equal("first\nsecond", module.Members[0].Header.DocComment!.Text);
    }

    [Fact]
    public void BlankLineSeparatesDocCommentFromFollowingDocComment()
    {
        // the second doc comment is dangling, as in the reference implementation
        Assert.Throws<PklParseException>(() => Parse("/// one\n\n/// two\nx = 1"));
    }

    [Fact]
    public void AnnotationsWithBodies()
    {
        var module = Parse("@Deprecated { message = \"old\" }\n@Unlisted\nx = 1");
        var annotations = module.Members[0].Header.Annotations;
        Assert.Equal(2, annotations.Count);
        Assert.NotNull(annotations[0].Body);
        Assert.Null(annotations[1].Body);
    }

    [Fact]
    public void ShebangIsAccepted()
    {
        Assert.Single(Parse("#!/usr/bin/env pkl eval\nx = 1").Members);
    }

    [Fact]
    public void SemicolonsAreIgnored()
    {
        Assert.Equal(2, Parse("x = 1; y = 2;").Members.Count);
    }

    [Fact]
    public void MinusOnNextLineStartsANewMember()
    {
        // `-` on a new line must not continue the previous expression
        Assert.Throws<PklParseException>(() => Parse("x = 1\n- 2"));
        Assert.Single(Parse("x = 1 - 2").Members);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("class Foo {")]
    [InlineData("x = ")]
    [InlineData("x = (1")]
    [InlineData("x: = 1")]
    [InlineData("local")]
    [InlineData("}")]
    public void SyntaxErrorsThrowParseExceptions(string source)
    {
        Assert.Throws<PklParseException>(() => Parse(source));
    }

    [Fact]
    public void ErrorSpanPointsAtOffendingToken()
    {
        var source = "a = 1\nb = )";
        var error = Assert.Throws<PklParseException>(() => Parse(source));
        Assert.Equal(source.IndexOf(')'), error.Span.Start);
    }

    [Fact]
    public void ErrorForMissingDelimiterPointsAtEndOfPreviousLine()
    {
        var source = "x = f(1\ny = 2";
        var error = Assert.Throws<PklParseException>(() => Parse(source));
        Assert.Equal(source.IndexOf('\n'), error.Span.Start);
    }

    [Fact]
    public void DeeplyNestedInputFailsGracefully()
    {
        var source = "x = " + new string('(', 5000) + "1" + new string(')', 5000);
        var error = Assert.Throws<PklParseException>(() => Parse(source));
        Assert.Contains("nested", error.Message);
    }
}
