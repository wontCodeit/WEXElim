namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDiscardPacket: IProcessedPacket
{
    public ProcessedDiscardPacket(byte senderId)
    {
        OpCode = OpCode.Discard;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}