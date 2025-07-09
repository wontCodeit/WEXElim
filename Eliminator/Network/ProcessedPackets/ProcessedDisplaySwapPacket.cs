namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDisplaySwapPacket: IProcessedPacket
{
    public ProcessedDisplaySwapPacket(byte senderId, ushort cardId1, ushort cardId2)
    {
        OpCode = OpCode.DisplaySwap;
        SenderId = senderId;
        CardId1 = cardId1;
        CardId2 = cardId2;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public ushort CardId1 { get; }
    public ushort CardId2 { get; }
}