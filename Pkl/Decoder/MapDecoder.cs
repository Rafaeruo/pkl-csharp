using MessagePack;

namespace Pkl.Decoding;

public partial class Decoder
{
    private object ReadMap(ref MessagePackReader reader)
    {
        var code = DecodePklObjectCode(ref reader);

        if (code == PklTypeCode.CodeSet)
        {
            return DecodeSet(ref reader);
        }

        if (code != PklTypeCode.CodeMap && code != PklTypeCode.CodeMapping)
        {
            throw new Exception("Invalid map code " + code);
        }

        return DecodeMap(ref reader);
    }

    private object DecodeSet(ref MessagePackReader reader)
    {
        throw new NotImplementedException();
    }

    private object DecodeMap(ref MessagePackReader reader)
    {
        throw new NotImplementedException();
    }
}