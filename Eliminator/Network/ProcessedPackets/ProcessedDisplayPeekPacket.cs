namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDisplayPeekPacket: IProcessedPacket
{
    public ProcessedDisplayPeekPacket(byte senderId, ushort cardId)
    {
        OpCode = OpCode.DisplayPeek;
        SenderId = senderId;
        CardId = cardId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public ushort CardId { get; }
}