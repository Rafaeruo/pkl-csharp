using PklGenerator.Declaration;
using PklGenerator.Parsing.Syntax;
using PklGenerator.Utilities;

namespace PklGenerator.Generation;

public sealed class TranslationResult
{
    public TranslationResult(IReadOnlyList<TypeDeclaration> types, IReadOnlyList<TranslationWarning> warnings)
    {
        Types = types;
        Warnings = warnings;
    }

    public IReadOnlyList<TypeDeclaration> Types { get; }
    public IReadOnlyList<TranslationWarning> Warnings { get; }
}

/// <summary>
/// Translates a parsed Pkl module into C# type declarations: the module itself becomes a class
/// named after the file, and every non-local Pkl class becomes a C# class in the same namespace.
/// </summary>
public static class ModuleTranslator
{
    /// <param name="generateEnums">
    /// Generate C# enums for <c>typealias</c>es that are unions of string literals, instead of using <c>string</c>.
    /// </param>
    public static TranslationResult Translate(
        PklModule module,
        string moduleName,
        string @namespace,
        bool generateEnums = false)
    {
        var warnings = new List<TranslationWarning>();
        var types = new List<TypeDeclaration>();

        var amends = module.ModuleDeclaration?.ExtendsOrAmends;
        if (amends is { Kind: ExtendsOrAmendsKind.Amends })
        {
            warnings.Add(new TranslationWarning(
                $"This module amends '{amends.Url.Value}' and only sets values, so no types were generated. "
                + "Generate types from the module being amended instead.",
                amends.Span));
            return new TranslationResult(types, warnings);
        }

        var classes = module.Members.OfType<ClassDecl>().Where(c => !c.Header.HasModifier(ModifierKind.Local)).ToList();
        var moduleClassName = Naming.ToTypeName(moduleName);

        // aliases the user mapped to an existing C# type with @CSharpType
        var customTypes = new Dictionary<string, string>();
        foreach (var alias in module.Members.OfType<TypeAliasDecl>())
        {
            var customType = CSharpTypeAnnotation.TryGetTypeName(alias, warnings);
            if (customType != null)
            {
                customTypes[alias.Name.Name] = customType;
            }
        }

        var enums = new Dictionary<string, StringUnionEnum>();
        if (generateEnums)
        {
            var takenNames = new HashSet<string>(classes.Select(c => Naming.ToTypeName(c.Name.Name))) { moduleClassName };
            foreach (var alias in module.Members.OfType<TypeAliasDecl>().Where(a => !customTypes.ContainsKey(a.Name.Name)))
            {
                var union = StringUnionEnum.TryCreate(alias);
                if (union == null)
                {
                    continue;
                }

                if (!takenNames.Add(union.TypeName))
                {
                    warnings.Add(new TranslationWarning(
                        $"The type alias '{alias.Name.Name}' was not generated as an enum because another generated type "
                        + $"is already called '{union.TypeName}', so it is treated as 'string' instead",
                        alias.Name.Span));
                    continue;
                }

                enums[alias.Name.Name] = union;
                types.Add(ToDeclaration(union, @namespace));
            }
        }

        var mapper = new TypeMapper(
            module.Members.OfType<TypeAliasDecl>(),
            classes.Select(c => c.Name.Name),
            ImportedModules(module),
            warnings,
            enums,
            customTypes);

        var moduleClass = new ClassDeclaration(moduleClassName, @namespace);
        if (amends is { Kind: ExtendsOrAmendsKind.Extends })
        {
            moduleClass.BaseType = ResolveModuleClassName(amends, warnings);
        }

        AddProperties(moduleClass, moduleClassName, module.Members.OfType<ClassPropertyDecl>(), mapper, @namespace);
        if (moduleClass.HasProperties || moduleClass.BaseType != null)
        {
            types.Add(moduleClass);
        }

        foreach (var classDecl in classes)
        {
            types.Add(TranslateClass(classDecl, @namespace, mapper, warnings));
        }

        return new TranslationResult(types, warnings);
    }

    private static EnumDeclaration ToDeclaration(StringUnionEnum union, string @namespace)
    {
        var declaration = new EnumDeclaration(union.TypeName, @namespace) { IsStringUnion = true };
        foreach (var (pklValue, memberName) in union.Members)
        {
            declaration.AddMember(memberName, pklValue);
        }

        return declaration;
    }

    private static ClassDeclaration TranslateClass(
        ClassDecl classDecl,
        string @namespace,
        TypeMapper mapper,
        List<TranslationWarning> warnings)
    {
        var typeName = Naming.ToTypeName(classDecl.Name.Name);
        var declaration = new ClassDeclaration(typeName, @namespace);
        if (classDecl.Header.HasModifier(ModifierKind.Abstract))
        {
            declaration.ClassKind = TypeKind.AbstractClass;
        }

        if (classDecl.Superclass != null)
        {
            var baseType = mapper.Map(classDecl.Superclass);
            // `Typed`, `Object` and unresolved types map to `object`, which isn't a useful base class
            if (baseType.Name != "object" && !baseType.IsValueType)
            {
                declaration.BaseType = baseType.Name;
            }
        }

        if (classDecl.TypeParameters != null)
        {
            warnings.Add(new TranslationWarning(
                $"Type parameters on class '{classDecl.Name.Name}' are ignored", classDecl.TypeParameters.Span));
        }

        var properties = classDecl.Body?.Members.OfType<ClassPropertyDecl>() ?? Enumerable.Empty<ClassPropertyDecl>();
        AddProperties(declaration, typeName, properties, mapper, @namespace);
        return declaration;
    }

    private static void AddProperties(
        ClassDeclaration declaration,
        string typeName,
        IEnumerable<ClassPropertyDecl> properties,
        TypeMapper mapper,
        string @namespace)
    {
        var usedNames = new HashSet<string> { typeName };
        foreach (var property in properties)
        {
            // local and hidden properties are never part of an evaluated module's output
            if (property.Header.HasModifier(ModifierKind.Local) || property.Header.HasModifier(ModifierKind.Hidden))
            {
                continue;
            }

            var type = ResolvePropertyType(property, mapper);
            var name = Naming.ToPropertyName(property.Name.Name);
            while (!usedNames.Add(name))
            {
                // C# forbids members named like their enclosing type, and Pkl names can collide after casing
                name += "_";
            }

            string? defaultValue = null;
            if (property.Value != null)
            {
                defaultValue = Literals.TryFormat(property.Value, type)
                    ?? mapper.TryFormatEnumDefault(type, property.Value, @namespace);
            }

            if (defaultValue == null && !type.IsValueType && !type.IsNullable)
            {
                // satisfy nullable analysis; the evaluator always assigns these
                defaultValue = "default!";
            }

            // the decoder finds properties by the Pkl name with its first letter upper-cased, so anything else
            // (sanitised or de-duplicated names) has to say which Pkl property it holds
            var pklName = property.Name.Name;
            var decoderName = char.ToUpperInvariant(pklName[0]) + pklName.Substring(1);
            declaration.AddProperty(new Property(
                name, type.Name, Visibility.Public, type.IsNullable, defaultValue, name == decoderName ? null : pklName));
        }
    }

    private static CSharpType ResolvePropertyType(ClassPropertyDecl property, TypeMapper mapper)
    {
        if (property.TypeAnnotation != null)
        {
            return mapper.Map(property.TypeAnnotation.Type);
        }

        switch (property.Value)
        {
            case null:
                return CSharpType.Object;
            case NewExpr { Type: { } newType }:
                return mapper.Map(newType);
            case var value:
                return Literals.Infer(value) ?? CSharpType.Object;
        }
    }

    /// <summary>
    /// Imports are bound to the file name (or alias) and the file is expected to be generated
    /// into the same namespace as a class with that name.
    /// </summary>
    private static Dictionary<string, string> ImportedModules(PklModule module)
    {
        var imports = new Dictionary<string, string>();
        foreach (var import in module.Imports)
        {
            if (import.IsGlob)
            {
                continue;
            }

            var className = ModuleClassNameFromUrl(import.Url.Value);
            if (className == null)
            {
                continue;
            }

            var binding = import.Alias?.Name ?? className;
            imports[binding] = Naming.ToTypeName(className);
        }

        return imports;
    }

    private static string? ResolveModuleClassName(ExtendsOrAmendsClause clause, List<TranslationWarning> warnings)
    {
        var className = ModuleClassNameFromUrl(clause.Url.Value);
        if (className == null)
        {
            warnings.Add(new TranslationWarning(
                $"Cannot determine a C# type for the module '{clause.Url.Value}', only relative .pkl files are supported",
                clause.Span));
            return null;
        }

        return Naming.ToTypeName(className);
    }

    private static string? ModuleClassNameFromUrl(string url)
    {
        if (!url.EndsWith(".pkl", StringComparison.OrdinalIgnoreCase) || url.Contains("://"))
        {
            return null;
        }

        var fileName = url.Substring(url.LastIndexOfAny(new[] { '/', '\\' }) + 1);
        return fileName.Substring(0, fileName.Length - ".pkl".Length);
    }
}
