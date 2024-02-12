using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[Union((int)Code.CodeNewEvaluatorResponse, typeof(CreateEvaluatorResponse))]
public abstract class IncomingMessageBase 
{
    protected abstract Code Code { get; set; }
}