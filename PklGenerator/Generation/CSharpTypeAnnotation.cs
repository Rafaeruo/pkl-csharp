using Microsoft.CodeAnalysis.CSharp;
using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Generation;

/// <summary>
/// Reads <c>@CSharpType { name = "System.Guid" }</c> on a <c>typealias</c>, which says that values of the alias
/// are an existing C# type. It is matched by the annotation class's name, so it works however the user brings
/// the class into the schema (the pkl-csharp Pkl package, a shared module, or a class they define themselves).
/// </summary>
internal static class CSharpTypeAnnotation
{
    public const string AnnotationName = "CSharpType";

    /// <summary>
    /// The fully qualified C# type named by the annotation, or null if there is none or it is not usable
    /// (in which case a warning is added).
    /// </summary>
    public static string? TryGetTypeName(TypeAliasDecl alias, List<TranslationWarning> warnings)
    {
        var annotation = alias.Header.Annotations.FirstOrDefault(a =>
            a.Type is DeclaredType declared && declared.Name.Last.Name == AnnotationName);
        if (annotation == null)
        {
            return null;
        }

        var nameProperty = annotation.Body?.Members
            .OfType<ObjectProperty>()
            .FirstOrDefault(p => p.Name.Name == "name");
        if (nameProperty?.Value is not StringLiteralExpr { ConstantValue: { } typeName })
        {
            warnings.Add(new TranslationWarning(
                $"The @{AnnotationName} annotation on '{alias.Name.Name}' needs a literal `name`, for example "
                + $"`@{AnnotationName} {{ name = \"System.Guid\" }}`, so the annotation is ignored",
                annotation.Span));
            return null;
        }

        if (!IsPlainTypeName(typeName, out var qualified))
        {
            warnings.Add(new TranslationWarning(
                $"'{typeName}' is not a plain C# type name (like 'System.Guid' or 'MyApp.Money'), so the "
                + $"@{AnnotationName} annotation on '{alias.Name.Name}' is ignored",
                annotation.Span));
            return null;
        }

        return qualified;
    }

    /// <summary>Dot-separated identifiers, optionally starting with <c>global::</c>; no generics, arrays or nullable.</summary>
    private static bool IsPlainTypeName(string name, out string qualified)
    {
        const string global = "global::";
        var bare = name.StartsWith(global, StringComparison.Ordinal) ? name.Substring(global.Length) : name;

        qualified = global + bare;
        return bare.Length > 0 && bare.Split('.').All(part => SyntaxFacts.IsValidIdentifier(part));
    }
}
