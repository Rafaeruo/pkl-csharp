using System.Runtime.CompilerServices;
using System.Text;
using PklGenerator.Declaration;

namespace PklGenerator.Utilities;

public enum TypeKind
{
    Enum,
    Class,
    SealedClass,
    StaticClass,
    Struct,
    RecordClass,
    RecordStruct,
}

public enum Visibility
{
    Public,
    Private,
    Internal,
}

public class CodeBuilder
{
    private const string IndentString = "    ";
    private readonly StringBuilder _builder;
    private int _indentLevel;

    public CodeBuilder(StringBuilder builder)
    {
        _builder = builder;

        builder.AppendLine("// <auto-generated />");
    }

    public void AddNamespace(string name)
    {
        StartLine().Append("namespace ").Append(name).AppendLine(";").AppendLine();
    }

    internal IndentScope EnterType(string name, TypeKind kind, Visibility visibility, string? baseType)
    {
        var kindString = ToKindString(kind);
        var visibilityString = ToVisibilityString(visibility);

        if (baseType == null)
        {
            StartLine().Append(visibilityString).Append(" ").Append(kindString).Append(" ").AppendLine(name);
        }
        else
        {
            StartLine().Append(visibilityString).Append(" ").Append(kindString).Append(" ").Append(name).Append(" : ").AppendLine(baseType);
        }
        
        StartLine().AppendLine("{");
        
        _indentLevel++;
        
        return new IndentScope(() =>
        {
            _indentLevel--;
            StartLine().AppendLine("}");
        });
    }

    public void AddEnumMember(string name)
    {
        StartLine().Append(name).AppendLine(",");
    }

    public void AddProperty(Property property)
    {
        var visibilityString = ToVisibilityString(property.Visibility);
        var nullableString = property.IsNullable ? "? " : " ";
        
        var line = StartLine()
            .Append(visibilityString)
            .Append(" ")
            .Append(property.Type)
            .Append(nullableString)
            .Append(property.Name)
            .Append(" { get; set; }");

        if (property.DefaultValue != null)
        {
            line.Append(" = ").Append(property.DefaultValue).AppendLine(";");
        }
        else
        {
            line.AppendLine();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StringBuilder StartLine()
    {
        for (var i = 0; i < _indentLevel; i++)
        {
            _builder.Append(IndentString);
        }

        return _builder;
    }

    private static string ToVisibilityString(Visibility visibility) => visibility switch
    {
        Visibility.Public => "public",
        Visibility.Private => "private",
        Visibility.Internal => "internal",

        _ => throw new ArgumentOutOfRangeException(nameof(visibility))
    };
    
    private static string ToKindString(TypeKind kind) => kind switch
    {
        TypeKind.Enum => "enum",
        TypeKind.Class => "class",
        TypeKind.SealedClass => "sealed class",
        TypeKind.StaticClass => "static class",
        TypeKind.Struct => "struct",
        TypeKind.RecordClass => "record",
        TypeKind.RecordStruct => "record struct",

        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    
    public readonly struct IndentScope : IDisposable
    {
        private readonly Action _action;

        public IndentScope(Action action)
        {
            _action = action;
        }

        public void Dispose() => _action();
    }
}