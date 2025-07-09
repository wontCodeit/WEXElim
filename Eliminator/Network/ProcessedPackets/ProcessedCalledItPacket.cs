namespace Eliminator.Network.ProcessedPackets;

public record ProcessedCalledItPacket: IProcessedPacket
{
    public ProcessedCalledItPacket(byte senderId)
    {
        OpCode = OpCode.CalledIt;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}