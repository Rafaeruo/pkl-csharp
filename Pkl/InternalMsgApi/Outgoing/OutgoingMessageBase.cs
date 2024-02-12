using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[Union((int)Code.CodeNewEvaluator, typeof(CreateEvaluator))]
public abstract class OutgoingMessageBase : IOutgoingMessage 
{
    protected abstract Code Code { get; set; }

    public byte[] ToMsgPack()
    {
        return MessagePackSerializer.Serialize(this);
    }
}