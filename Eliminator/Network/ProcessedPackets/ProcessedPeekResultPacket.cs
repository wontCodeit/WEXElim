namespace Eliminator.Network.ProcessedPackets;

public record ProcessedPeekResultPacket: IProcessedPacket
{
    public ProcessedPeekResultPacket(byte senderId, CardValue cardValue)
    {
        OpCode = OpCode.PeekResult;
        SenderId = senderId;
        CardValue = cardValue;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public CardValue CardValue { get; }
}