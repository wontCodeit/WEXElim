namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDrawPacket: IProcessedPacket
{
    public ProcessedDrawPacket(byte senderId)
    {
        OpCode = OpCode.Draw;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}