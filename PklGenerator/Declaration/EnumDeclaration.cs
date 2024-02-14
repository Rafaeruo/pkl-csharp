using PklGenerator.Utilities;

namespace PklGenerator.Declaration;

internal class EnumDeclaration : TypeDeclaration
{
    private readonly List<string> _members = new List<string>();

    public EnumDeclaration(string typeName, string enumNamespace): base(typeName)
    {
        TypeName = typeName;
        Namespace = enumNamespace;
    }
    
    public string TypeName { get; }
    public string Namespace;

    public void AddMember(string name)
    {
        _members.Add(name);
    }

    protected override void BuildCode(CodeBuilder builder)
    {
        builder.AddNamespace(Namespace);
        using var typeScope = builder.EnterType(TypeName, TypeKind.Enum, Visibility.Public, null);
        
        foreach (var member in _members)
        {
            builder.AddEnumMember(member);
        }
    }
}