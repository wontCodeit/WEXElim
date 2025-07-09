namespace Eliminator.Network.ProcessedPackets;

public record ProcessedStartTurnPacket: IProcessedPacket
{
    public ProcessedStartTurnPacket(byte senderId, byte playerId)
    {
        OpCode = OpCode.StartTurn;
        SenderId = senderId;
        PlayerId = playerId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public byte PlayerId { get; }
}