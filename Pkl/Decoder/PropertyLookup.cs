using System.Collections.Concurrent;
using System.Reflection;

namespace Pkl.Decoding;

public partial class Decoder
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> mappedProperties = new();

    /// <summary>
    /// Finds the C# property that holds a Pkl property: one marked <see cref="PklPropertyAttribute"/> with that
    /// name, otherwise the property with the Pkl name's first letter upper-cased.
    /// </summary>
    private PropertyInfo? FindProperty(Type targetType, string pklName)
    {
        var mapped = mappedProperties.GetOrAdd(targetType, BuildMappedProperties);
        if (mapped.TryGetValue(pklName, out var property))
        {
            return property;
        }

        return targetType.GetProperty(ToPropertyName(pklName));
    }

    private static IReadOnlyDictionary<string, PropertyInfo> BuildMappedProperties(Type type)
    {
        var mapped = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attribute = property.GetCustomAttribute<PklPropertyAttribute>();
            if (attribute != null)
            {
                mapped[attribute.Name] = property;
            }
        }

        return mapped;
    }
}
