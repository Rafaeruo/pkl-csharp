using PklCSharp.Generator.Utilities;

namespace PklCSharp.Generator.Declaration;

public class ClassDeclaration : TypeDeclaration
{
    private TypeKind _classKind = TypeKind.Class;
    private readonly List<Property> _properties = new List<Property>();
    
    public ClassDeclaration(string typeName, string classNamespace) : base(typeName)
    {
        TypeName = typeName;
        Namespace = classNamespace;
    }
    
    public string TypeName { get; }
    public string Namespace;
    public string? BaseType = null;
    public Visibility Visibility = Visibility.Public;
    
    public TypeKind ClassKind
    {
        get => _classKind;
        set
        {
            if (value == TypeKind.Enum)
            {
                throw new ArgumentException("Use an EnumDeclaration for enum kinds");
            }

            _classKind = value;
        }
    }

    public void AddProperty(Property property)
    {
        _properties.Add(property);
    }

    protected override void BuildCode(CodeBuilder builder)
    {
        builder.AddNamespace(Namespace);
        using var typeScope = builder.EnterType(TypeName, ClassKind, Visibility, BaseType);

        foreach (var property in _properties)
        {
            builder.AddProperty(property);
        }
    }
}
