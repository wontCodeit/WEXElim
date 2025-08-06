namespace Eliminator.Network.ProcessedPackets;

public record ProcessedQuickPlaceResultPacket: IProcessedPacket
{
    public ProcessedQuickPlaceResultPacket(byte senderId, QuickPlaceResult result, byte playerId, CardValue cardValue, ushort cardId)
    {
        OpCode = OpCode.QuickPlaceResult;
        SenderId = senderId;
        Result = result;
        PlayerId = playerId;
        CardValue = cardValue;
        CardId = cardId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public QuickPlaceResult Result { get; }
    public byte PlayerId { get; }
    public CardValue CardValue { get; }
    public ushort CardId { get; }
}