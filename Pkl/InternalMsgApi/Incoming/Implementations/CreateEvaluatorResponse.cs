using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[MessagePackObject]
public class CreateEvaluatorResponse : IncomingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("error")]
    public string? Error { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeNewEvaluatorResponse;
}