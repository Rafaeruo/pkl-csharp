using MessagePack;

namespace Pkl.Decoding;

public partial class Decoder
{
    private readonly Dictionary<string, Type> _typeMappings;

    public Decoder(Dictionary<string, Type> typeMappings)
    {
        _typeMappings = typeMappings;
    }

    public T Decode<T>(byte[] input) where T : notnull
    {
        var reader = new MessagePackReader(input);
        
        return DecodeAny<T>(ref reader);
    }

    private T DecodeAny<T>(ref MessagePackReader reader) where T : notnull
    {
        var value = DecodeAny(ref reader, typeof(T));

        if (value is T valueT)
        {
            return valueT;
        }

        throw new Exception("Invalid type parameter " + nameof(T));
    }

    private object? DecodeAny(ref MessagePackReader reader, Type targetType)
    {
        var messagePackType = reader.NextMessagePackType;
        var code = reader.NextCode;

        switch (messagePackType)
        {
            case MessagePackType.Map:
                return ReadMap(ref reader, targetType);
            case MessagePackType.Array:
                return ReadPklObject(ref reader, targetType);
            case MessagePackType.String:
                return DecodeString(reader.ReadString()!, targetType);
            case MessagePackType.Nil:
                reader.Skip();
                return null;
            // Primitives
            case MessagePackType.Boolean:
                return reader.ReadBoolean();
            case MessagePackType.Integer:
                return DecodeInteger(ref reader, targetType);
            case MessagePackType.Float:
                if (code == MessagePackCode.Float32)
                {
                    return (double)reader.ReadSingle();
                }

                return reader.ReadDouble();
            default:
                throw new Exception("Unsupported messagepackcode");
        }
    }

    /// <summary>
    /// Pkl's Int is always a 64-bit signed integer. MessagePack encodes it in the smallest width that fits
    /// (fixint, uint8, uint16, ...), which says nothing about the type the value should have in C#, so the
    /// value is read as 64 bits and converted to the declared target type.
    /// </summary>
    private static object DecodeInteger(ref MessagePackReader reader, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (reader.NextCode == MessagePackCode.UInt64)
        {
            // Not produced by Pkl (its Int is signed), but valid MessagePack
            var unsigned = reader.ReadUInt64();
            if (unsigned <= long.MaxValue)
            {
                return ConvertInteger((long)unsigned, underlying);
            }

            return underlying == typeof(ulong) || underlying == typeof(object)
                ? unsigned
                : throw IntegerDoesNotFit(unsigned, underlying);
        }

        return ConvertInteger(reader.ReadInt64(), underlying);
    }

    private static object ConvertInteger(long value, Type target)
    {
        if (target == typeof(object) || target == typeof(long))
        {
            return value;
        }

        try
        {
            if (target.IsEnum)
            {
                return Enum.ToObject(target, value);
            }

            return Type.GetTypeCode(target) switch
            {
                TypeCode.SByte => checked((sbyte)value),
                TypeCode.Byte => checked((byte)value),
                TypeCode.Int16 => checked((short)value),
                TypeCode.UInt16 => checked((ushort)value),
                TypeCode.Int32 => checked((int)value),
                TypeCode.UInt32 => checked((uint)value),
                TypeCode.UInt64 => checked((ulong)value),
                TypeCode.Single => (float)value,
                TypeCode.Double => (double)value,
                TypeCode.Decimal => (decimal)value,
                // not a numeric target: hand back the Pkl Int and let the caller report the mismatch
                _ => value,
            };
        }
        catch (OverflowException)
        {
            throw IntegerDoesNotFit(value, target);
        }
    }

    private static OverflowException IntegerDoesNotFit(object value, Type target) =>
        new($"The Pkl value {value} does not fit in {target.Name}.");
}
