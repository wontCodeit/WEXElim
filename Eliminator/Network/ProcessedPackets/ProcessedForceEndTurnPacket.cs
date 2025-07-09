namespace Eliminator.Network.ProcessedPackets;

public record ProcessedForceEndTurnPacket: IProcessedPacket
{
    public ProcessedForceEndTurnPacket(byte senderId)
    {
        OpCode = OpCode.ForceEndTurn;
        SenderId = senderId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
}