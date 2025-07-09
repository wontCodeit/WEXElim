namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDiscardResultPacket: IProcessedPacket
{
    public ProcessedDiscardResultPacket(byte senderId, CardValue cardValue)
    {
        OpCode = OpCode.DiscardResult;
        SenderId = senderId;
        CardValue = cardValue;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public CardValue CardValue { get; }
}