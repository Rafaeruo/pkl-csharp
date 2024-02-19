using MessagePack;

namespace Pkl.Decoding;

public partial class Decoder
{
    private object ReadMap(ref MessagePackReader reader, Type targetType)
    {
        var code = DecodePklObjectCode(ref reader);

        if (code == PklTypeCode.CodeSet)
        {
            return DecodeSet(ref reader, targetType);
        }

        if (code != PklTypeCode.CodeMap && code != PklTypeCode.CodeMapping)
        {
            throw new Exception("Invalid map code " + code);
        }

        return DecodeMap(ref reader, targetType);
    }

    private object DecodeSet(ref MessagePackReader reader, Type targetType)
    {
        var genericArguments = targetType.GetGenericArguments();
        if (genericArguments.Length != 1)
        {
            throw new Exception("Invalid generic arguments for set type");
        }

        var len = reader.ReadArrayHeader();
        var hashSetType = typeof(HashSet<>).MakeGenericType(genericArguments[0]);
        var hashSet = Activator.CreateInstance(hashSetType, len);

        var invokeParams = new object[1];
        for (int i = 0; i < len; i++)
        {
            var item = DecodeAny(ref reader, genericArguments[0]);
            invokeParams[0] = item!;
            hashSetType.GetMethod(nameof(HashSet<object>.Add))!.Invoke(hashSet, invokeParams);
        }

        return hashSet!;
    }

    private object DecodeMap(ref MessagePackReader reader, Type targetType)
    {
        var genericArguments = targetType.GetGenericArguments();
        if (genericArguments.Length != 2)
        {
            throw new Exception("Invalid generic arguments for map type");
        }

        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(genericArguments);
        var dictionary = Activator.CreateInstance(dictionaryType);

        var len = reader.ReadMapHeader();
        var invokeParams = new object[2];
        for (int i = 0; i < len; i++)
        {
            var key = DecodeAny(ref reader, genericArguments[0]);
            var value = DecodeAny(ref reader, genericArguments[1]);

            invokeParams[0] = key!;
            invokeParams[1] = value!;
            dictionaryType.GetMethod(nameof(Dictionary<object, object>.Add))!.Invoke(dictionary, invokeParams);
        }

        return dictionary!;
    }
}