using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Pkl.Decoding;

public partial class Decoder
{
    /// <summary>
    /// Pkl strings are plain strings unless the target type asks for more. Pkl's <c>Uri</c> is
    /// <c>String(isValidUrl)</c> and its <c>Char</c> is <c>String(length == 1)</c>, so both arrive as strings
    /// and are converted here, as are <see cref="PklStringUnionAttribute"/> enums, <see cref="DateTime"/>, <see cref="Version"/> and any type that implements
    /// <see cref="IParsable{TSelf}"/>.
    /// </summary>
    private static object DecodeString(string value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(Uri))
        {
            return DecodeUri(value);
        }

        if (underlying == typeof(char))
        {
            return DecodeChar(value);
        }

        if (underlying.IsEnum)
        {
            return DecodeStringUnion(value, underlying);
        }

        if (underlying == typeof(DateTime))
        {
            // keep the kind of an ISO 8601 string: "...Z" stays UTC instead of becoming local time
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime)
                ? dateTime
                : throw NotParsable(value, underlying);
        }

        if (underlying == typeof(Version))
        {
            // Version has TryParse(string, out Version) but does not implement IParsable
            return Version.TryParse(value, out var version) ? version : throw NotParsable(value, underlying);
        }

        var parser = GetParser(underlying);
        if (parser != null)
        {
            var arguments = new object?[] { value, CultureInfo.InvariantCulture, null };
            return (bool)parser.Invoke(null, arguments)!
                ? arguments[2]!
                : throw NotParsable(value, underlying);
        }

        return value;
    }

    // null entries record types that are not parsable, so the reflection only happens once per type
    private static readonly ConcurrentDictionary<Type, MethodInfo?> parsers = new();

    /// <summary>
    /// The static <c>TryParse(string, IFormatProvider, out T)</c> of a type that implements <c>IParsable&lt;T&gt;</c>.
    /// Primitive types (and decimal) are left out: a Pkl string is not silently accepted for an int property.
    /// </summary>
    private static MethodInfo? GetParser(Type type) => parsers.GetOrAdd(type, static t =>
    {
        if (t == typeof(string) || t == typeof(object) || t.IsPrimitive || t == typeof(decimal))
        {
            return null;
        }

        // not MakeGenericType: that throws for types that don't satisfy the interface's constraint
        var parsable = t.GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParsable<>) && i.GenericTypeArguments[0] == t);
        if (parsable == null)
        {
            return null;
        }

        // the implementation may be explicit, which GetMethod would not find
        var map = t.GetInterfaceMap(parsable);
        for (var i = 0; i < map.InterfaceMethods.Length; i++)
        {
            if (map.InterfaceMethods[i].Name == nameof(IParsable<int>.TryParse))
            {
                return map.TargetMethods[i];
            }
        }

        return null;
    });

    private static FormatException NotParsable(string value, Type type) =>
        new($"The Pkl value \"{value}\" is not a valid {type.Name}.");

    private static Uri DecodeUri(string value)
    {
        // Relative references are valid Pkl URLs too
        if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
        {
            return uri;
        }

        throw new FormatException($"The Pkl value \"{value}\" is not a valid URI.");
    }

    private static char DecodeChar(string value)
    {
        if (value.Length == 1)
        {
            return value[0];
        }

        throw new FormatException(
            $"The Pkl value \"{value}\" cannot be a single char. " +
            "(Pkl counts characters as Unicode code points, and a C# char holds one UTF-16 code unit.)");
    }
}
