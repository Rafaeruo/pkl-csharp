namespace Pkl;

/// <summary>
/// Marks an enum as the C# form of a Pkl union of string literals, such as
/// <c>typealias Status = "active" | "in-progress"</c>. The decoder converts the Pkl string into the
/// matching member. Use <see cref="PklStringUnionValueAttribute"/> on members whose Pkl string is not
/// simply their name.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class PklStringUnionAttribute : Attribute
{
}

/// <summary>
/// The Pkl string a member of a <see cref="PklStringUnionAttribute"/> enum stands for. Members without
/// this attribute match their own name exactly.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class PklStringUnionValueAttribute : Attribute
{
    public PklStringUnionValueAttribute(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>The string as written in Pkl, for example <c>"in-progress"</c>.</summary>
    public string Value { get; }
}
