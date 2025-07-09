namespace Eliminator.Test;
public class DeckTests
{
    /// <summary>
    /// GIVEN: No pre-conditions
    /// WHEN: A deck object is instantiated
    /// THEN: The stored card values are stored in a random order
    /// </summary>
    [Fact]
    public void ShuffleTest()
    {
        Deck deck1 = new(1);
        Deck deck2 = new(1);
        var areEqual = true;
        for (var i = 0; i < deck1.Remaining; i++)
        {
            if (deck1.Draw() != deck2.Draw())
            {
                areEqual = false;
                break;
            }
        }

        // The probability that by chance both decks' orders have been randomised
        // Into the exact same order is ridiculously, ludicrously, incomprehensibly small
        // It would take 1 eternity of running this test for it to give a false negative
        Assert.False(areEqual);
    }

    /// <summary>
    /// GIVEN: No pre-conditions
    /// WHEN: A deck has been instantiated
    /// THEN: It contains 54 unique standard playing cards
    /// </summary>
    [Fact]
    public void FullDeckTest()
    {
        Deck deck1 = new(1);
        List<CardValue> cardsInDeck = [];
        for (var i = 0; i < 54; i++)
        {
            cardsInDeck.Add(deck1.Draw());
        }

        Assert.Equal(54, cardsInDeck.Distinct().Count());
    }
}
