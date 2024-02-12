using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<Evaluate>))]
public class Evaluate : OutgoingMessageBase
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("moduleUri")]
    public string? ModuleUri { get; set; }

    [Key("moduleText")]
    public string? ModuleText { get; set; }

    [Key("expr")]
    public string? Expr { get; set; }

    protected override Code Code { get; set; } = Code.CodeEvaluate;
}