using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[Union((int)Code.CodeNewEvaluator, typeof(CreateEvaluator))]
[Union((int)Code.CodeEvaluate, typeof(Evaluate))]
[Union((int)Code.CodeCloseEvaluator, typeof(CloseEvaluator))]
[Union((int)Code.CodeEvaluateReadResponse, typeof(ReadResourceResponse))]
[Union((int)Code.CodeListResourcesResponse, typeof(ListResourcesResponse))]
[Union((int)Code.CodeEvaluateReadModuleResponse, typeof(ReadModuleResponse))]
[Union((int)Code.CodeListModulesResponse, typeof(ListModulesResponse))]
public abstract class OutgoingMessageBase : IOutgoingMessage 
{
    protected abstract Code Code { get; set; }

    public byte[] ToMsgPack()
    {
        return MessagePackSerializer.Serialize(this);
    }
}