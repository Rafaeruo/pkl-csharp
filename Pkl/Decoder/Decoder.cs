using MessagePack;

namespace Pkl.Decoding;

public class Decoder
{
    public object Decode(byte[] input)
    {
        var reader = new MessagePackReader(input);
        // TODO
        return null!;
    }
}