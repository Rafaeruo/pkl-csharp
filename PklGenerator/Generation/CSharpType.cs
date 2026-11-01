namespace PklGenerator.Generation;

/// <param name="Name">The C# type without a trailing <c>?</c>.</param>
internal sealed record CSharpType(string Name, bool IsValueType, bool IsNullable = false)
{
    public static readonly CSharpType Object = new("object", false);
    public static readonly CSharpType String = new("string", false);

    public CSharpType AsNullable() => this with { IsNullable = true };

    /// <summary>The full type as it would be written in C#, including <c>?</c> when nullable.</summary>
    public string Render() => IsNullable ? Name + "?" : Name;
}
