using MessagePack;
using Pkl.PklTypes;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Pkl.Decoding;

public partial class Decoder
{
    private object? ReadPklObject(ref MessagePackReader reader, Type targetType)
    {
        var code = DecodePklObjectCode(ref reader);

        return code switch
        {
            PklTypeCode.CodeObject => DecodeObject(ref reader, targetType),
            PklTypeCode.CodeMap or PklTypeCode.CodeMapping => DecodeMap(ref reader, targetType),
            PklTypeCode.CodeSet => DecodeSet(ref reader, targetType),
            PklTypeCode.CodeList or PklTypeCode.CodeListing => DecodeCollection(ref reader, targetType),
            PklTypeCode.CodeDataSize => DecodeDataSize(ref reader),
            PklTypeCode.CodeDuration => DecodeDuration(ref reader),
            PklTypeCode.CodePair => DecodePair(ref reader, targetType),
            PklTypeCode.CodeIntSeq => DecodeIntSeq(ref reader),
            PklTypeCode.CodeRegex => DecodeRegex(ref reader),
            PklTypeCode.CodeClass => DecodeClass(),
            PklTypeCode.CodeTypeAlias => DecodeTypeAlias(),

            _ => throw new Exception(nameof(code) + " is out of range"),
        };
    }

    private PklTypeCode DecodePklObjectCode(ref MessagePackReader reader)
    {
        var _ = reader.ReadArrayHeader();
        var code = reader.ReadInt32();

        return (PklTypeCode)code;
    }

    private object? DecodeObject(ref MessagePackReader reader, Type targetType)
    {
        var name = reader.ReadString();
        var moduleUri = reader.ReadString();

        var isDynamic = targetType == typeof(object)
            || moduleUri == "pkl:base" && name == "Dynamic";
        if (isDynamic)
        {
            return DecodeDynamic(ref reader, targetType);
        }

        var instance = Activator.CreateInstance(targetType);
        var len = reader.ReadArrayHeader();
        for (int i = 0; i < len; i++)
        {
            var code = DecodePklObjectCode(ref reader);

            if (code != PklTypeCode.CodeObjectMemberProperty)
            {
                continue;
            }

            var propertyName = reader.ReadString()!;
            propertyName =  ToPropertyName(propertyName);
            var property = targetType.GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            var propertyType = property.PropertyType;
            var propertyValue = DecodeAny(ref reader, propertyType);
            property.SetValue(instance, propertyValue);
        }

        return instance;
    }

    private dynamic DecodeDynamic(ref MessagePackReader reader, Type targetType)
    {
        var len = reader.ReadArrayHeader();
        var instance = new ExpandoObject() as IDictionary<string, object?>;
        for (int i = 0; i < len; i++)
        {
            var code = DecodePklObjectCode(ref reader);

            if (code != PklTypeCode.CodeObjectMemberProperty)
            {
                continue;
            }

            var propertyName = reader.ReadString()!;
            propertyName = ToPropertyName(propertyName);
            var propertyValue = DecodeAny(ref reader, typeof(object));

            instance.Add(propertyName, propertyValue);
        }

        return instance;
    }

    private DataSize DecodeDataSize(ref MessagePackReader reader)
    {
        var value = reader.ReadDouble();
        var unitString = reader.ReadString();
        var unit = DataSizeUnitFromString(unitString);

        return new DataSize(unit, value);
    }

    private DataSizeUnit DataSizeUnitFromString(string? unitString)
    {
        return unitString?.ToLower() switch
        {
            "b" => DataSizeUnit.Bytes,
            "kb" => DataSizeUnit.Kilobytes,
            "kib" => DataSizeUnit.Kibibytes,
            "mb" => DataSizeUnit.Megabytes,
            "mib" => DataSizeUnit.Mebibytes,
            "gb" => DataSizeUnit.Gigabytes,
            "gib" => DataSizeUnit.Gibibytes,
            "tb" => DataSizeUnit.Terabytes,
            "tib" => DataSizeUnit.Tebibytes,
            "pb" => DataSizeUnit.Petabytes,
            "pib" => DataSizeUnit.Pebibytes,
            _ => throw new ArgumentOutOfRangeException(nameof(unitString))
        };
    }

    private Duration DecodeDuration(ref MessagePackReader reader)
    {
        var value = reader.ReadDouble();
        var unitString = reader.ReadString();
        var unit = DurationUnitFromString(unitString);

        return new Duration(unit, value);
    }

    private DurationUnit DurationUnitFromString(string? unitString)
    {
        return unitString switch
        {
            "ns" => DurationUnit.Nanosecond,
            "us" => DurationUnit.Microsecond,
            "ms" => DurationUnit.Millisecond,
            "s" => DurationUnit.Second,
            "min" => DurationUnit.Minute,
            "h" => DurationUnit.Hour,
            "d" => DurationUnit.Day,

            _ => throw new ArgumentOutOfRangeException(nameof(unitString)),
        };
    }

    private object? DecodePair(ref MessagePackReader reader, Type targetType)
    {
        var genericArguments = targetType.GetGenericArguments();
        var firstType = genericArguments.First();
        var secondType = genericArguments.Last();

        var first = DecodeAny(ref reader, firstType);
        var second = DecodeAny(ref reader, secondType);

        var tupleType = typeof(ValueTuple<,>).MakeGenericType(firstType, secondType);
        var tuple = Activator.CreateInstance(tupleType, first, second);
        
        return tuple;
    }

    private IntSeq DecodeIntSeq(ref MessagePackReader reader)
    {
        var start = reader.ReadInt32();
        var end = reader.ReadInt32();
        var step = reader.ReadInt32();

        return new IntSeq(start, end, step);
    }

    private Regex DecodeRegex(ref MessagePackReader reader)
    {
        var pattern = reader.ReadString();

        return new Regex(pattern!);
    }

    private Class DecodeClass()
    {
        return Class.Instance;
    }

    private TypeAlias DecodeTypeAlias()
    {
        return TypeAlias.Instance;
    }

    private string ToPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        if (propertyName.Length == 1)
        {
            return char.ToUpper(propertyName[0]).ToString();
        }

        return char.ToUpper(propertyName[0]) + propertyName.Substring(1);;
    }
}