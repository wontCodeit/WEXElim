namespace Eliminator.Network.ProcessedPackets;

public record ProcessedPeekResultPacket: IProcessedPacket
{
    public ProcessedPeekResultPacket(byte senderId, CardValue cardValue, ushort cardId)
    {
        OpCode = OpCode.PeekResult;
        SenderId = senderId;
        CardValue = cardValue;
        CardId = cardId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public CardValue CardValue { get; }
    public ushort CardId { get; }
}