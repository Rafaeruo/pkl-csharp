using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[MessagePackObject]
public class Log : IncomingMessageBase
{
    [Key("evaluatorId")]
    public long EvaluatorId { get; set; }

    [Key("level")]
    public int Level { get; set; }

    [Key("message")]
    public string Message { get; set; } = default!;

    [Key("frameUri")]
    public string FrameUri { get; set; } = default!;
    
    protected override Code Code { get; set; } = Code.CodeEvaluateLog;

}