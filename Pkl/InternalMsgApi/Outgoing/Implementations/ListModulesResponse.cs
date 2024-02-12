using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<ListModulesResponse>))]
public class ListModulesResponse : OutgoingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("pathElements")]
    public List<PathElement>? PathElements { get; set; }

    [Key("error")]
    public string? Error { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeListModulesResponse;
}

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<PathElement>))]
public class PathElement
{
    [Key("name")]
    public required string Name { get; set; }

    [Key("isDirectory")]
    public bool IsDirectory { get; set; }
}