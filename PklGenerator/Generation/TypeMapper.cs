using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Generation;

/// <summary>Maps Pkl types to the C# types the runtime decoder can produce.</summary>
internal sealed class TypeMapper
{
    private const string Generic = "global::System.Collections.Generic.";
    private const string PklTypes = "global::Pkl.PklTypes.";

    private readonly Dictionary<string, TypeAliasDecl> _aliases;
    private readonly IReadOnlyDictionary<string, StringUnionEnum> _enums;
    private readonly IReadOnlyDictionary<string, string> _customTypes;
    private readonly HashSet<string> _classes;
    private readonly Dictionary<string, string> _importedModules;
    private readonly List<TranslationWarning> _warnings;
    private readonly HashSet<string> _aliasesBeingResolved = new();

    public TypeMapper(
        IEnumerable<TypeAliasDecl> aliases,
        IEnumerable<string> classNames,
        Dictionary<string, string> importedModules,
        List<TranslationWarning> warnings,
        IReadOnlyDictionary<string, StringUnionEnum>? enums = null,
        IReadOnlyDictionary<string, string>? customTypes = null)
    {
        _enums = enums ?? new Dictionary<string, StringUnionEnum>();
        _customTypes = customTypes ?? new Dictionary<string, string>();
        _aliases = new Dictionary<string, TypeAliasDecl>();
        foreach (var alias in aliases)
        {
            _aliases[alias.Name.Name] = alias;
        }

        _classes = new HashSet<string>(classNames);
        _importedModules = importedModules;
        _warnings = warnings;
    }

    public CSharpType Map(TypeNode type)
    {
        switch (type)
        {
            case ParenthesizedType parenthesized:
                return Map(parenthesized.Inner);
            case ConstrainedType constrained:
                return Map(constrained.Inner);
            case NullableType nullable:
                return Map(nullable.Inner).AsNullable();
            case StringLiteralType:
                return CSharpType.String;
            case UnionType union:
                return MapUnion(union);
            case DeclaredType declared:
                return MapDeclared(declared);
            case FunctionType:
            case UnknownType:
            case NothingType:
            case ModuleType:
            case ThisType:
            default:
                return CSharpType.Object;
        }
    }

    private CSharpType MapUnion(UnionType union)
    {
        var members = union.Members.Select(Unwrap).ToList();
        var isNullable = members.Any(IsNullType);
        var remaining = members.Where(m => !IsNullType(m)).Select(Map).ToList();

        CSharpType result;
        if (remaining.Count == 0)
        {
            result = CSharpType.Object;
        }
        else if (remaining.All(t => t.Name == remaining[0].Name))
        {
            // e.g. a union of string literals, or `String|String`
            result = remaining[0];
        }
        else
        {
            result = CSharpType.Object;
        }

        return isNullable || remaining.Any(t => t.IsNullable) ? result.AsNullable() : result;
    }

    private static TypeNode Unwrap(TypeNode type)
    {
        while (type is ParenthesizedType parenthesized)
        {
            type = parenthesized.Inner;
        }

        return type;
    }

    private static bool IsNullType(TypeNode type) =>
        Unwrap(type) is DeclaredType { Name.Parts.Count: 1 } declared && declared.Name.Last.Name == "Null";

    private CSharpType MapDeclared(DeclaredType declared)
    {
        var name = declared.Name.Last.Name;
        var isQualified = declared.Name.Parts.Count > 1;
        var arguments = declared.TypeArguments?.Arguments ?? Array.Empty<TypeNode>();

        if (!isQualified && _customTypes.TryGetValue(name, out var customType))
        {
            // the user said what this alias is; whether it is a class or a struct doesn't matter to the
            // generated property (`= default!` is fine for both)
            return new CSharpType(customType, false);
        }

        if (!isQualified && _enums.TryGetValue(name, out var union))
        {
            return new CSharpType(union.TypeName, true, union.IsNullable);
        }

        if (!isQualified && _aliases.TryGetValue(name, out var alias))
        {
            if (!_aliasesBeingResolved.Add(name))
            {
                return CSharpType.Object;
            }

            try
            {
                return Map(alias.Type);
            }
            finally
            {
                _aliasesBeingResolved.Remove(name);
            }
        }

        if (!isQualified && _classes.Contains(name))
        {
            return new CSharpType(Naming.ToTypeName(name), false);
        }

        if (_importedModules.TryGetValue(declared.Name.Parts[0].Name, out var moduleClass))
        {
            // `Imported` is the module's own class, `Imported.Foo` is a class declared in it
            return new CSharpType(isQualified ? Naming.ToTypeName(name) : moduleClass, false);
        }

        var standard = MapStandard(name, arguments);
        if (standard != null)
        {
            return standard;
        }

        _warnings.Add(new TranslationWarning(
            $"Cannot resolve the Pkl type '{declared.Name}', using 'object' instead", declared.Span));
        return CSharpType.Object;
    }

    /// <summary>
    /// The initializer for a property of a generated enum type whose Pkl default is one of the enum's
    /// string literals, or null. The enum is fully qualified so a property in the class can't shadow it.
    /// </summary>
    public string? TryFormatEnumDefault(CSharpType type, Expr value, string @namespace)
    {
        while (value is ParenthesizedExpr parenthesized)
        {
            value = parenthesized.Inner;
        }

        if (value is not StringLiteralExpr { ConstantValue: { } text })
        {
            return null;
        }

        var union = _enums.Values.FirstOrDefault(e => e.TypeName == type.Name);
        var member = union?.FindMember(text);
        return member == null ? null : $"global::{@namespace}.{union!.TypeName}.{member}";
    }

    private CSharpType? MapStandard(string name, IReadOnlyList<TypeNode> arguments)
    {
        switch (name)
        {
            case "String":
                return CSharpType.String;
            case "Boolean":
                return new CSharpType("bool", true);
            case "Int":
                return new CSharpType("long", true);
            case "Int8":
                return new CSharpType("sbyte", true);
            case "Int16":
                return new CSharpType("short", true);
            case "Int32":
                return new CSharpType("int", true);
            case "UInt":
                return new CSharpType("ulong", true);
            case "UInt8":
                return new CSharpType("byte", true);
            case "UInt16":
                return new CSharpType("ushort", true);
            case "UInt32":
                return new CSharpType("uint", true);
            case "Float":
            case "Number":
                return new CSharpType("double", true);
            case "Any":
            case "Dynamic":
            case "Object":
            case "Typed":
            case "Null":
            case "Class":
            case "TypeAlias":
                return CSharpType.Object;
            case "Uri":
                // Pkl's Uri is a String, which the decoder converts when the target is a System.Uri
                return new CSharpType("global::System.Uri", false);
            case "Char":
                return new CSharpType("char", true);
            case "Bytes":
                return new CSharpType("byte[]", false);
            case "Duration":
                return new CSharpType(PklTypes + "Duration", true);
            case "DataSize":
                return new CSharpType(PklTypes + "DataSize", true);
            case "IntSeq":
                return new CSharpType(PklTypes + "IntSeq", true);
            case "Regex":
                return new CSharpType("global::System.Text.RegularExpressions.Regex", false);
            case "List":
            case "Listing":
            case "Collection":
                // the decoder materializes lists and listings as arrays
                return new CSharpType(Element(arguments, 0).Render() + "[]", false);
            case "Set":
                return new CSharpType($"{Generic}HashSet<{Element(arguments, 0).Render()}>", false);
            case "Map":
            case "Mapping":
                return new CSharpType(
                    $"{Generic}Dictionary<{Element(arguments, 0).Render()}, {Element(arguments, 1).Render()}>", false);
            case "Pair":
                return new CSharpType(
                    $"global::System.ValueTuple<{Element(arguments, 0).Render()}, {Element(arguments, 1).Render()}>",
                    true);
            default:
                return null;
        }
    }

    private CSharpType Element(IReadOnlyList<TypeNode> arguments, int index) =>
        index < arguments.Count ? Map(arguments[index]) : CSharpType.Object;
}
