using System.Collections.Concurrent;
using System.Reflection;

namespace Pkl.Decoding;

public partial class Decoder
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, object>> stringUnionValues = new();

    /// <summary>
    /// Converts a Pkl string to a <see cref="PklStringUnionAttribute"/> enum member. The target is known to be
    /// an enum (or a nullable one).
    /// </summary>
    private static object DecodeStringUnion(string value, Type underlying)
    {
        var values = stringUnionValues.GetOrAdd(underlying, BuildStringUnionValues);
        if (values.Count == 0 && underlying.GetCustomAttribute<PklStringUnionAttribute>() is null)
        {
            throw new NotSupportedException(
                $"Cannot decode the Pkl string \"{value}\" into the enum {underlying.Name}. " +
                $"Mark the enum with [PklStringUnion] (and its members with [PklStringUnionValue] where the Pkl string " +
                $"is not the member name) to decode Pkl string unions into it.");
        }

        if (values.TryGetValue(value, out var member))
        {
            return member;
        }

        var accepted = string.Join(", ", values.Keys.Select(k => $"\"{k}\""));
        throw new FormatException(
            $"The Pkl value \"{value}\" is not one of the values of the enum {underlying.Name} ({accepted}).");
    }

    private static IReadOnlyDictionary<string, object> BuildStringUnionValues(Type enumType)
    {
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        if (enumType.GetCustomAttribute<PklStringUnionAttribute>() is null)
        {
            return values;
        }

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var pklValue = field.GetCustomAttribute<PklStringUnionValueAttribute>()?.Value ?? field.Name;
            values[pklValue] = field.GetValue(null)!;
        }

        return values;
    }
}
