namespace Eliminator.Network.ProcessedPackets;

public record ProcessedCallItPacket: IProcessedPacket
{
    public ProcessedCallItPacket(byte senderId)
    {
        OpCode = OpCode.CallIt;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}