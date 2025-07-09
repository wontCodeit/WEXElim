namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDisplayDrawPacket: IProcessedPacket
{
    public ProcessedDisplayDrawPacket(byte senderId)
    {
        OpCode = OpCode.DisplayDraw;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}