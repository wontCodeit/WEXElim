namespace Eliminator.Network.ProcessedPackets;

public record ProcessedPassTurnPacket: IProcessedPacket
{
    public ProcessedPassTurnPacket(byte senderId)
    {
        OpCode = OpCode.PassTurn;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}