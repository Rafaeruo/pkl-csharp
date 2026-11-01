using Microsoft.CodeAnalysis.CSharp;
using PklGenerator.Utilities;

namespace PklGenerator.Declaration;

public class EnumDeclaration : TypeDeclaration
{
    private readonly List<(string Name, string? PklValue)> _members = new List<(string, string?)>();

    public EnumDeclaration(string typeName, string enumNamespace): base(typeName)
    {
        TypeName = typeName;
        Namespace = enumNamespace;
    }
    
    public string TypeName { get; }
    public string Namespace;

    /// <summary>
    /// When set, the enum is generated as the form of a Pkl union of string literals: it is marked
    /// <c>[PklStringUnion]</c> and members added with a Pkl value carry it in <c>[PklStringUnionValue]</c>.
    /// </summary>
    public bool IsStringUnion { get; set; }

    public void AddMember(string name)
    {
        _members.Add((name, null));
    }

    public void AddMember(string name, string pklValue)
    {
        _members.Add((name, pklValue));
    }

    protected override void BuildCode(CodeBuilder builder)
    {
        builder.AddNamespace(Namespace);
        if (IsStringUnion)
        {
            builder.AddAttribute("global::Pkl.PklStringUnion");
        }

        using var typeScope = builder.EnterType(TypeName, TypeKind.Enum, Visibility.Public, null);
        
        foreach (var (name, pklValue) in _members)
        {
            if (IsStringUnion && pklValue != null)
            {
                builder.AddAttribute(
                    "global::Pkl.PklStringUnionValue(" + SymbolDisplay.FormatLiteral(pklValue, quote: true) + ")");
            }

            builder.AddEnumMember(name);
        }
    }
}