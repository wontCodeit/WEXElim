namespace Eliminator.Network.ProcessedPackets;

public record ProcessedScramblePacket: IProcessedPacket
{
    public ProcessedScramblePacket(byte senderId, byte playerId)
    {
        OpCode = OpCode.Scramble;
        SenderId = senderId;
        PlayerId = playerId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public byte PlayerId { get; }
}