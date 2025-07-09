namespace Eliminator.Network.ProcessedPackets;

public record ProcessedQuickPlacePacket: IProcessedPacket
{
    public ProcessedQuickPlacePacket(byte senderId, ushort cardId, CardValue seenDiscard)
    {
        OpCode = OpCode.QuickPlace;
        SenderId = senderId;
        CardId = cardId;
        SeenDiscard = seenDiscard;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public ushort CardId { get; }
    public CardValue SeenDiscard { get; }
}