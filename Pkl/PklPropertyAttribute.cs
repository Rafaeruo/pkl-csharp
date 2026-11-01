namespace Pkl;

/// <summary>
/// The Pkl property a C# property holds. Without it, a Pkl property <c>host</c> is matched to the C# property
/// <c>Host</c> (the first letter upper-cased). Use it when the C# name has to differ, for example for the Pkl
/// property <c>my-prop</c>, or for a property that cannot share the name of its class.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PklPropertyAttribute : Attribute
{
    public PklPropertyAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>The property's name in Pkl, for example <c>my-prop</c>.</summary>
    public string Name { get; }
}
