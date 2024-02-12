using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[MessagePackObject]
public class EvaluateResponse : IncomingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("result")]
    public byte[]? Result { get; set; }

    [Key("error")]
    public string? Error { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeEvaluateResponse;
}