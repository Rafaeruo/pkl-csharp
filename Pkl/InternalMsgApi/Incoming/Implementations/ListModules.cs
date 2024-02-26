using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[MessagePackObject]
public class ListModules : IncomingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("uri")]
    public string Uri { get; set; } = default!;
    
    protected override Code Code { get; set; } = Code.CodeListModulesRequest;
}