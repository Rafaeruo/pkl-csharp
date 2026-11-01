using System.Buffers;
using MessagePack;
using Pkl.Decoding;

namespace Pkl.Tests.Decoding;

/// <summary>
/// The pkl-binary spec says decoders must discard values beyond the known slots of a type
/// and newer Pkl releases add properties, slots and types. These guard against that.
/// </summary>
public class ForwardCompatibilityTests
{
    public class Target
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    private delegate void WriteAction(ref MessagePackWriter w);

    private static byte[] Encode(WriteAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        write(ref writer);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteClass(ref MessagePackWriter w, int extraSlots)
    {
        w.WriteArrayHeader(1 + extraSlots);
        w.Write(0x0C);
        for (var i = 0; i < extraSlots; i++) w.Write("slot" + i);
    }

    [Fact]
    public void UnknownPropertiesAndClassValuesAreSkipped()
    {
        var bytes = Encode((ref MessagePackWriter w) =>
        {
            w.WriteArrayHeader(4);              // object: [code, name, module, members]
            w.Write(0x01);
            w.Write("Target");
            w.Write("file:///t.pkl");
            w.WriteArrayHeader(4);

            w.WriteArrayHeader(3);              // unknown property holding a Class with name+module slots (>= 0.27 shape)
            w.Write(0x10);
            w.Write("newProp");
            WriteClass(ref w, extraSlots: 2);

            w.WriteArrayHeader(4);              // known property carrying an extra trailing slot
            w.Write(0x10);
            w.Write("name");
            w.Write("hello");
            w.Write("future-slot");

            w.WriteArrayHeader(3);              // unknown property holding a nil
            w.Write(0x10);
            w.Write("rewrites");
            w.WriteNil();

            w.WriteArrayHeader(3);
            w.Write(0x10);
            w.Write("count");
            w.Write(7);
        });

        var result = new Decoder(new()).Decode<Target>(bytes);

        Assert.Equal("hello", result.Name);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void BytesValuesDecodeToByteArray()
    {
        var bytes = Encode((ref MessagePackWriter w) =>
        {
            w.WriteArrayHeader(2);
            w.Write(0x0F);
            w.Write(new byte[] { 1, 2, 3 });
        });

        var result = new Decoder(new()).Decode<byte[]>(bytes);

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void UnknownTypeCodeGivesClearError()
    {
        var bytes = Encode((ref MessagePackWriter w) =>
        {
            w.WriteArrayHeader(2);
            w.Write(0x7E);
            w.WriteNil();
        });

        var ex = Assert.Throws<NotSupportedException>(() => new Decoder(new()).Decode<object>(bytes));
        Assert.Contains("0x7E", ex.Message);
    }
}
