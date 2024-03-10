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
                return reader.ReadString()!;
            case MessagePackType.Nil:
                reader.Skip();
                return null;
            // Primitives
            case MessagePackType.Boolean:
                return reader.ReadBoolean();
            case MessagePackType.Integer:
                if (code == MessagePackCode.UInt8)
                {
                    return reader.ReadByte();
                }

                if (code == MessagePackCode.Int8)
                {
                    return reader.ReadSByte();
                }

                if (code == MessagePackCode.UInt16)
                {
                    return reader.ReadUInt16();
                }

                if (code == MessagePackCode.Int16)
                {
                    return reader.ReadInt16();
                }

                if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                {
                    return code;
                }
                
                if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                {
                    return unchecked((sbyte)code);
                }

                if (code == MessagePackCode.UInt32)
                {
                    return reader.ReadUInt32();
                }

                if (code == MessagePackCode.Int32)
                {
                    return reader.ReadInt32();
                }

                if (code == MessagePackCode.UInt64)
                {
                    return reader.ReadUInt64();
                }

                return reader.ReadInt64();
            case MessagePackType.Float:
                if (code == MessagePackCode.Float32)
                {
                    reader.ReadSingle();  
                }

                return reader.ReadDouble();
            default:
                throw new Exception("Unsupported messagepackcode");
        }
    }
}
