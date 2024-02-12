using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<ListResourcesResponse>))]
public class ListResourcesResponse : OutgoingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("pathElements")]
    public List<PathElement>? PathElements { get; set; }

    [Key("error")]
    public string? Error { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeListResourcesResponse;
}