namespace Eliminator.Network.ProcessedPackets;

public record ProcessedGameEndPacket: IProcessedPacket
{
    public ProcessedGameEndPacket(byte senderId, List<(byte, int)> scores)
    {
        OpCode = OpCode.GameEnd;
        SenderId = senderId;
        Scores = scores;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public List<(byte, int)> Scores { get; }
}