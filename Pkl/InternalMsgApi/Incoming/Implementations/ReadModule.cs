using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[MessagePackObject]
public class ReadModule : IncomingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("uri")]
    public required string Uri { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeEvaluateReadModule;
}