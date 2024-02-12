using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<ReadResourceResponse>))]
public class ReadResourceResponse : OutgoingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("contents")]
    public byte[]? Contents { get; set; }

    [Key("error")]
    public string? Error { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeEvaluateReadResponse;
}