using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace PklGenerator.Generation;

internal static class Naming
{
    /// <summary>Turns an arbitrary string into a valid C# identifier (without keyword escaping).</summary>
    public static string Sanitize(string name)
    {
        if (name.Length == 0)
        {
            return "_";
        }

        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    public static string ToTypeName(string pklName)
    {
        var name = Sanitize(pklName);
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }

    /// <summary>
    /// Pkl property names become PascalCase C# properties. This mirrors how the runtime decoder
    /// looks properties up (it upper-cases the first character), so the two must stay in sync.
    /// </summary>
    public static string ToPropertyName(string pklName)
    {
        var name = Sanitize(pklName);
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public static string ToNamespace(string? candidate, string fallback)
    {
        if (candidate != null
            && candidate.Length > 0
            && candidate.Split('.').All(part => SyntaxFacts.IsValidIdentifier(part)))
        {
            return candidate;
        }

        return fallback;
    }
}
