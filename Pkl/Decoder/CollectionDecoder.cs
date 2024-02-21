using MessagePack;

namespace Pkl.Decoding;

public partial class Decoder
{
    // TODO maybe support collection types other than arrays
    private Array DecodeCollection(ref MessagePackReader reader, Type targetType)
    {
        var len = reader.ReadArrayHeader();
        var elementType = targetType.GetElementType();

        if (elementType is null)
        {
            throw new Exception(nameof(targetType) + "must be an array with an element type");
        }

        var arr = Array.CreateInstance(elementType, len);
        for (int i = 0; i < len; i++)
        {
            var value = DecodeAny(ref reader, elementType);
            arr.SetValue(value, i);
        }

        return arr;
    }
}