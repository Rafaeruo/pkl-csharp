using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<ReadModuleResponse>))]
public class ReadModuleResponse : OutgoingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("contents")]
    public string? Contents { get; set; }

    [Key("error")]
    public string? Error { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeEvaluateReadModuleResponse;
}