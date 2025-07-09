namespace Eliminator.Test;
public class HandManagerTests
{
    private const int TOTAL_DECK_VALUE = 334;

    /// <summary>
    /// GIVEN: no pre-conditions
    /// WHEN: A HandManager is instantiated
    /// THEN: Every player has a unique index, every card has a unique index, each count up sequentially from zero
    /// </summary>
    [Fact]
    public void HandIndexingTest()
    {
        Card.Reset();
        HandManager handManager = new(5, 5, new Deck(1));
        List<byte> playerIndexes = handManager.PlayerIds();
        List<ushort> cardIndexes = [];
        playerIndexes.ForEach(i =>
        {
            cardIndexes.AddRange(
                handManager.GetCardsInHand(i).Select(card => card.Id));
        });

        Assert.Equal([0, 1, 2, 3, 4], playerIndexes);
        Assert.Equal(25, cardIndexes.Distinct().Count());
        Assert.Equal(2, cardIndexes.First()); // accounting for placeholder values
        Assert.Equal(26, cardIndexes.Last());
    }

    /// <summary>
    /// GIVEN: The QuickPlace that will occur is valid
    /// WHEN: A QuickPlace occurs
    /// THEN: The placed card is discarded from the player's hand and a true result is returned
    /// </summary>
    [Fact]
    public void QuickPlaceValidTest()
    {
        Card.Reset();
        HandManager handManager = new(1, 2, new Deck(1));
        List<Card> cards = handManager.GetCardsInHand(0);
        var cardIds = cards.Select(card => card.Id).ToList();
        var cardToPlace = cardIds[0];
        var cardValue = (CardValue)Card.GetNumber(cardToPlace)!;
        handManager.ToDiscard(cardValue);

        Assert.True(handManager.QuickPlace(cardIds[0]));
        Assert.DoesNotContain(
            cardToPlace,
            handManager.GetCardsInHand(0).Select(card => card.Id).ToList());
    }

    /// <summary>
    /// GIVEN: The QuickPlace that will occur is not valid
    /// WHEN: A QuickPlace occurs
    /// THEN: The placed card is not discarded from the player's hand and a false result is returned
    /// </summary>
    [Fact]
    public void QuickPlaceInvalidTest()
    {
        Card.Reset();
        HandManager handManager = new(1, 2, new Deck(1));
        var cardIds = handManager.GetCardsInHand(0).Select(card => card.Id).ToList();
        var cardToPlace = cardIds[0];
        var cardValue = (CardValue)Card.GetNumber((ushort)(cardToPlace + 1))!;
        handManager.ToDiscard(cardValue);

        Assert.False(handManager.QuickPlace(cardIds[0]));
        Assert.Contains(
            cardToPlace,
            handManager.GetCardsInHand(0).Select(card => card.Id).ToList());
    }

    /// <summary>
    /// GIVEN: A player hand has greater than 1 card
    /// WHEN: A scramble occurs
    /// THEN: The player's hand has its card values swapped amongst themselves randomly
    /// such that the values at each id cannot be determined without additional information
    /// </summary>
    [Fact]
    public void ScrambleTest()
    {
        Card.Reset();
        HandManager handManager = new(1, 54, new Deck(1));
        var cardIds = handManager.GetCardsInHand(0).Select(card => card.Id).ToList();
        List<(ushort, CardValue)> cards = [];

        cards.Clear();
        cardIds.ForEach(id =>
        {
            cards.Add((id, (CardValue)Card.GetNumber(id)!));
        });

        handManager.Scramble(0);

        List<(int, CardValue)> newCardPairings = [];
        cardIds.ForEach(id =>
        {
            newCardPairings.Add((id, (CardValue)Card.GetNumber(id)!));
        });

        var areEqual = true;
        for (var i = 0; i < newCardPairings.Count; i++)
        {
            if (newCardPairings[i] != cards[i])
            {
                areEqual = false;
                break;
            }
        }

        Assert.False(areEqual);
    }

    /// <summary>
    /// GIVEN: There are two players, each with 1 card in hand
    /// WHEN: A swap occurs swapping cards held in player hands
    /// THEN: Each player's selected card has its <see cref="CardValue"/> swapped with the other player's <see cref="CardValue"/>
    /// </summary>
    [Fact]
    public void SwapTest()
    {
        Card.Reset();
        HandManager handManager = new(2, 1, new Deck(1));
        Card card1 = handManager.GetCardsInHand(0).First();
        Card card2 = handManager.GetCardsInHand(1).First();
        var card1OriginalVal = (CardValue)card1.Number!;
        var card2OriginalVal = (CardValue)card2.Number!;
        handManager.Swap(card1.Id, card2.Id);
        Assert.True(card1.Number == card2OriginalVal);
        Assert.True(card2.Number == card1OriginalVal);
    }

    /// <summary>
    /// GIVEN: There is a single hand containing every card
    /// WHEN: The game ends and points are tallied
    /// THEN: Every hand's point values are calculated accurately according to Eliminator rules
    /// </summary>
    [Fact]
    public void HandHasValueTest()
    {
        Card.Reset();
        HandManager handManager = new(1, 54, new Deck(1));

        Assert.Equal(0, handManager.CalculateHandValues().First().Item1);
        Assert.Equal(TOTAL_DECK_VALUE, handManager.CalculateHandValues().First().Item2);
    }

    /// <summary>
    /// GIVEN: There are cards left in the deck
    /// WHEN: A card is drawn
    /// THEN: It is stored for later reference
    /// </summary>
    [Fact]
    public void DrawStoredAsHeldTest()
    {
        Card.Reset();
        HandManager handManager = new(1, 4, new Deck(1));
        Assert.Null(Card.GetNumber(handManager.HeldCardId));

        handManager.DrawCard();

        CardValue? number = Card.GetNumber(handManager.HeldCardId);

        Assert.NotNull(number);
    }

    /// <summary>
    /// GIVEN: There is a drawn card and a player has 1 card in hand
    /// WHEN: A swap occurs between the drawn card and the card held in hand
    /// THEN: Each cards value is swapped
    /// </summary>
    [Fact]
    public void SwapHeldTest()
    {
        Card.Reset();
        HandManager handManager = new(2, 4, new Deck(1));
        handManager.DrawCard();
        var drawnValue = (CardValue)Card.GetNumber(handManager.HeldCardId)!;
        Card cardToSwap = handManager.GetCardsInHand(0).First();
        CardValue? handCardValue = cardToSwap.Number;
        handManager.Swap(cardToSwap.Id, handManager.HeldCardId);
        Assert.Equal(drawnValue, cardToSwap.Number);
        Assert.Equal(handCardValue, (CardValue)Card.GetNumber(handManager.HeldCardId)!);
    }
}
