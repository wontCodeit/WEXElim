namespace Eliminator.Network.ProcessedPackets;

public class ProcessedPeekPacket: IProcessedPacket
{
    public ProcessedPeekPacket(byte senderId, ushort cardId)
    {
        OpCode = OpCode.Peek;
        SenderId = senderId;
        CardId = cardId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public ushort CardId { get; }
}