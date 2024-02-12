using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<CloseEvaluator>))]
public class CloseEvaluator : OutgoingMessageBase
{
    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    protected override Code Code { get; set; } = Code.CodeCloseEvaluator;
}