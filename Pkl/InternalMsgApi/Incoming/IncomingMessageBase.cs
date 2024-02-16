using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[Union((int)Code.CodeNewEvaluatorResponse, typeof(CreateEvaluatorResponse))]
[Union((int)Code.CodeEvaluateResponse, typeof(EvaluateResponse))]
public abstract class IncomingMessageBase 
{
    protected abstract Code Code { get; set; }
}