namespace Pkl.Decoding;

public partial class Decoder
{
    /// <summary>
    /// Pkl strings are plain strings unless the target type asks for more. Pkl's <c>Uri</c> is
    /// <c>String(isValidUrl)</c> and its <c>Char</c> is <c>String(length == 1)</c>, so both arrive as strings
    /// and are converted here, as are <see cref="PklStringUnionAttribute"/> enums.
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

        return value;
    }

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
