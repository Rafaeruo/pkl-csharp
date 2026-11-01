using System.Text;
using PklGenerator.Parsing.Syntax;

namespace PklGenerator.Generation;

/// <summary>A Pkl <c>typealias</c> of string literals, such as <c>typealias Status = "a" | "b"</c>, as a C# enum.</summary>
internal sealed class StringUnionEnum
{
    public StringUnionEnum(string typeName, bool isNullable, IReadOnlyList<(string PklValue, string MemberName)> members)
    {
        TypeName = typeName;
        IsNullable = isNullable;
        Members = members;
    }

    public string TypeName { get; }

    /// <summary>The alias included <c>Null</c>, so properties of this type are nullable.</summary>
    public bool IsNullable { get; }

    public IReadOnlyList<(string PklValue, string MemberName)> Members { get; }

    public string? FindMember(string pklValue) =>
        Members.Where(m => m.PklValue == pklValue).Select(m => m.MemberName).FirstOrDefault();

    /// <summary>
    /// Returns the enum for an alias whose body consists only of string literals (and optionally <c>Null</c>),
    /// otherwise null.
    /// </summary>
    public static StringUnionEnum? TryCreate(TypeAliasDecl alias)
    {
        if (alias.TypeParameters != null)
        {
            return null;
        }

        var values = new List<string>();
        var nullable = false;
        if (!Collect(alias.Type, values, ref nullable) || values.Count == 0)
        {
            return null;
        }

        var used = new HashSet<string>();
        var members = new List<(string, string)>();
        foreach (var value in values.Distinct())
        {
            var name = ToMemberName(value);
            while (!used.Add(name))
            {
                name += "_";
            }

            members.Add((value, name));
        }

        return new StringUnionEnum(Naming.ToTypeName(alias.Name.Name), nullable, members);
    }

    private static bool Collect(TypeNode type, List<string> values, ref bool nullable)
    {
        switch (type)
        {
            case ParenthesizedType parenthesized:
                return Collect(parenthesized.Inner, values, ref nullable);
            case NullableType nullableType:
                nullable = true;
                return Collect(nullableType.Inner, values, ref nullable);
            case StringLiteralType literal:
                values.Add(literal.Value.Value);
                return true;
            case DeclaredType { Name.Parts.Count: 1 } declared when declared.Name.Last.Name == "Null":
                nullable = true;
                return true;
            case UnionType union:
                foreach (var member in union.Members)
                {
                    if (!Collect(member, values, ref nullable))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// <c>"in-progress"</c> becomes <c>InProgress</c>: words are split at anything that is not a letter or digit,
    /// and each starts with a capital. The Pkl string itself is kept in an attribute, so this only has to be valid.
    /// </summary>
    internal static string ToMemberName(string value)
    {
        var builder = new StringBuilder();
        var startOfWord = true;
        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c))
            {
                startOfWord = true;
                continue;
            }

            builder.Append(startOfWord ? char.ToUpperInvariant(c) : c);
            startOfWord = false;
        }

        if (builder.Length == 0)
        {
            return value.Length == 0 ? "Empty" : "Value";
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }
}
