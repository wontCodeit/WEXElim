namespace Eliminator.Network.ProcessedPackets;

public record ProcessedQuickPlaceResultPacket: IProcessedPacket
{
    public ProcessedQuickPlaceResultPacket(byte senderId, QuickPlaceResult result, byte playerId, CardValue cardValue)
    {
        OpCode = OpCode.QuickPlaceResult;
        SenderId = senderId;
        Result = result;
        PlayerId = playerId;
        CardValue = cardValue;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public QuickPlaceResult Result { get; }
    public byte PlayerId { get; }
    public CardValue CardValue { get; }
}