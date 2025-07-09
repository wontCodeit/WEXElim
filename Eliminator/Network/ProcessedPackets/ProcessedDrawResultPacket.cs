namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDrawResultPacket: IProcessedPacket
{
    public ProcessedDrawResultPacket(byte senderId, CardValue cardValue)
    {
        OpCode = OpCode.DrawResult;
        SenderId = senderId;
        CardValue = cardValue;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public CardValue CardValue { get; }
}