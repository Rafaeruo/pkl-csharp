using MessagePack;

namespace Pkl.Decoding;

public partial class Decoder
{
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
                return ReadMap(ref reader);
            case MessagePackType.Array:
                return ReadPklObject(ref reader, targetType);
            case MessagePackType.String:
                return reader.ReadString()!;
            case MessagePackType.Nil:
                reader.Skip();
                return null;
            // Primitives
            case MessagePackType.Boolean:
                return reader.ReadBoolean();
            case MessagePackType.Integer:
                // TODO differentiate integer types
                if (code is MessagePackCode.Int8 
                    or MessagePackCode.Int16
                    or MessagePackCode.UInt8
                    or MessagePackCode.UInt16
                    or (>= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt)
                    or (>= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt))
                {
                    return reader.ReadInt16();
                }

                if (code is MessagePackCode.Int32 or MessagePackCode.UInt32)
                {
                    return reader.ReadInt32();
                }

                if (code is MessagePackCode.Int64 or MessagePackCode.UInt64)
                {
                    return reader.ReadInt64();
                }
                
                throw new Exception("Invalid integer code " + code);
            case MessagePackType.Float:
                // TODO differentiate floats and doubles
                return reader.ReadDouble();
            default:
                throw new Exception("Unsupported messagepackcode");
        }
    }
}