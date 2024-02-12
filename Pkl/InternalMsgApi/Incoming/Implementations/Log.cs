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
    public required string Message { get; set; }

    [Key("frameUri")]
    public required string FrameUri { get; set; }
    
    protected override Code Code { get; set; } = Code.CodeEvaluateLog;

}