namespace Pkl.InternalMsgApi.Outgoing;

public interface IOutgoingMessage 
{
	byte[] ToMsgPack();
}
