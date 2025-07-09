namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDisplayScramblePacket: IProcessedPacket
{
    public ProcessedDisplayScramblePacket(byte senderId, byte playerId)
    {
        OpCode = OpCode.DisplayScramble;
        SenderId = senderId;
        PlayerId = playerId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public byte PlayerId { get; }
}