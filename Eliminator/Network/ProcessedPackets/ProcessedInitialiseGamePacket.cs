namespace Eliminator.Network.ProcessedPackets;
public record ProcessedInitialiseGamePacket: IProcessedPacket
{
    public ProcessedInitialiseGamePacket(byte senderId, int startingCards, int deckSize, CardValue initialDiscard, int turnTimeLimit, List<(byte, string)> players)
    {
        OpCode = OpCode.InitialiseGame;
        SenderId = senderId;
        StartingCards = startingCards;
        Players = players;
        DeckSize = deckSize;
        TurnTimeLimit = turnTimeLimit;
        InitialDiscard = initialDiscard;
    }

    public OpCode OpCode { get; }

    public byte SenderId { get; }

    public int StartingCards { get; }

    public int DeckSize { get; }

    public CardValue InitialDiscard { get; }

    public int TurnTimeLimit { get; }

    public List<(byte, string)> Players { get; }
}
