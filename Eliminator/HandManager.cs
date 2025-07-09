using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Eliminator;

/// <summary>
/// Class that controls the cards, and therefore the hands, in an Eliminator game
/// </summary>
public class HandManager
{
    private readonly Stack<CardValue> _discardBuffer = [];
    //                                  PlayerId       CardId
    private readonly ReadOnlyDictionary<byte, List<Card>> _playerHands;
    public IDeck Deck { get; private set; }

    /// <summary>
    /// The currently drawn card held by a player- a placeholder id in <see cref="Card"/>
    /// </summary>
    public ushort HeldCardId { get; }

    /// <summary>
    /// The card currently on the top of the discard pile- a placeholder id in <see cref="Card"/>
    /// </summary>
    public ushort TopDiscardCardId { get; }

    /// <summary>
    /// The number of remaining cards
    /// </summary>
    public int RemainingCards => Deck.Remaining;

    public (byte playerCount, int startingCards, int deckSize) InitialisationInfo { get; private set; }

    /// <summary>
    /// Constructor for a <see cref="HandManager"/>.
    /// </summary>
    /// <param name="playerCount"> How many players to track hands for </param>
    /// <param name="startingCards"> The initial hand of cards each is dealt </param>
    /// <param name="deck"> The <see cref="Eliminator.Deck"/> that this <see cref="HandManager"/> should use </param>
    public HandManager(byte playerCount, int startingCards, IDeck deck)
    {
        Debug.Assert(playerCount > 0
            && startingCards > 0,
            "Can't play with no cards/players");
        Debug.Assert(playerCount * startingCards <= deck.Remaining,
            "Cannot deal out more cards than there are in the deck");

        HeldCardId = Card.AddPlaceholder();
        TopDiscardCardId = Card.AddPlaceholder();

        InitialisationInfo = (playerCount, startingCards, deck.StandardSizeMultiple);
        Dictionary<byte, List<Card>> tempDict = [];
        for (byte i = 0; i < playerCount; i++)
        {
            var someDict = new List<Card>();
            for (var j = 0; j < startingCards; j++)
            {
                someDict.Add(new(deck.Draw()));
            }

            tempDict.Add(i, someDict);
        }

        _playerHands = tempDict.AsReadOnly();
        Deck = deck;
    }

    /// <summary>
    /// Adds the given <see cref="CardValue"/> to the discard pile
    /// </summary>
    /// <param name="cardValue"> the value to push </param>
    public void ToDiscard(CardValue cardValue)
    {
        _discardBuffer.Push(cardValue);
        Card.ChangePlaceholderNumber(TopDiscardCardId, _discardBuffer.Peek());
    }

    /// <summary>
    /// Draws from the deck and sets the HeldCards value to it
    /// </summary>
    /// <exception cref="InvalidOperationException"> Thrown if deck has no cards left </exception>
    public void DrawCard()
    {
        Card.ChangePlaceholderNumber(HeldCardId, Deck.Draw());
    }

    /// <summary>
    /// Send the value of HeldCard to the discard pile
    /// </summary>
    /// <returns></returns>
    public void DiscardHeldCard()
    {
        CardValue? cardValue = Card.GetNumber(HeldCardId);
        Debug.Assert(cardValue != null, "Can't DiscardHeldCard when nothing is drawn");
        ToDiscard((CardValue)cardValue);
        Card.ChangePlaceholderNumber(HeldCardId, null);
    }

    /// <summary>
    /// Gets the list of player ids stored (synonymous with hand ids)
    /// </summary>
    /// <returns> The player ids stored in this <see cref="HandManager"/></returns>
    public List<byte> PlayerIds()
    {
        return [.. _playerHands.Keys];
    }

    /// <summary>
    /// Gets the ids of every card in the given player's hand
    /// </summary>
    /// <param name="playerId"> Id of the player who should have their hand looked at </param>
    /// <returns> The id of every card in the given player's hand </returns>
    /// <exception cref="ArgumentException"> Thrown when the player can't be found </exception>
    public List<Card> GetCardsInHand(byte playerId)
    {
        return _playerHands.TryGetValue(playerId, out List<Card>? hand)
            ? hand
            : throw new ArgumentException($"Player hand with id: {playerId} could not be found");
    }

    #region Swap Methods
    /// <summary>
    /// Swaps the <see cref="CardValue"/> that the two specified cards have, NOT the card ids
    /// </summary>
    /// <param name="cardOneId"> The id of one card to swap </param>
    /// <param name="cardTwoId"> The id of the other card to swap </param>
    /// <exception cref="InvalidOperationException"> If the given card ids don't conform to any expected swap, exception thrown </exception>
    /// <exception cref="ArgumentException"> If any given card id is not found, exception thrown </exception>
    public void Swap(ushort cardOneId, ushort cardTwoId)
    {
        /* Possible paths:
        1. Neither card is a placeholder (both are held in hand(s))
        2. One of the cards is the Drawn Card and the other is the Discard Card (discard the drawn card)
        3. One of the cards is the Discard Card and the other is held in a hand (take from discard and replace one in hand)
        4. One of the cards is the Drawn Card and the other is held in a hand (take the drawn card and replace one in hand) 
        */

        //Handle path 1
        if (cardOneId != HeldCardId &&
            cardOneId != TopDiscardCardId &&
            cardTwoId != HeldCardId &&
            cardTwoId != TopDiscardCardId)
        {
            SwapNoPlaceholders(cardOneId, cardTwoId);
            return;
        }

        //Handle path 2
        if ((cardOneId == TopDiscardCardId || cardTwoId == TopDiscardCardId) &&
            (cardOneId == HeldCardId || cardTwoId == HeldCardId))
        {
            SwapBothPlaceholders();
            return;
        }

        //Handle path 3
        if (cardOneId == TopDiscardCardId ||
           cardTwoId == TopDiscardCardId)
        {
            var notDiscardCard = cardOneId == TopDiscardCardId
                ? cardTwoId
                : cardOneId;

            SwapWithDiscard(notDiscardCard);
            return;
        }

        // Handle path 4
        if (cardOneId == HeldCardId ||
            cardTwoId == HeldCardId)
        {
            var notHeldCard = cardOneId == HeldCardId
                ? cardTwoId
                : cardOneId;

            SwapWithHeldCard(notHeldCard);
            return;
        }

        throw new InvalidOperationException("Unexpected ids entered, no possible swap exists");
    }

    /// <summary>
    /// Swap a <see cref="Card"/> in a hand with the currently drawn <see cref="Card"/>
    /// </summary>
    /// <param name="cardId"> The id of the <see cref="Card"/> that is in a hand </param>
    private void SwapWithHeldCard(ushort cardId)
    {
        Card? card = null;

        foreach (List<Card> hand in _playerHands.Values)
        {
            card = hand.FirstOrDefault(card => card.Id == cardId);
            if (card != null)
            {
                break;
            }
        }

        if (card == null)
        {
            throw new InvalidOperationException($"Could not find the card with given id: {cardId}");
        }

        if (Card.GetNumber(HeldCardId) is null)
        {
            throw new InvalidOperationException($"No held card exists");
        }

        var heldCardVal = (CardValue)Card.GetNumber(HeldCardId)!;
        Card.ChangePlaceholderNumber(HeldCardId, card?.Number);
        card?.ChangeNumber(heldCardVal);
    }

    /// <summary>
    /// Swap a <see cref="Card"/> that is in a hand with the top of the discard pile (discarding it)
    /// </summary>
    /// <param name="cardId"> the id of the <see cref="Card"/> that is in a hand </param>
    private void SwapWithDiscard(ushort cardId)
    {
        Card? card = null;

        foreach (List<Card> hand in _playerHands.Values)
        {
            card = hand.FirstOrDefault(card => card.Id == cardId);
        }

        if (card == null)
        {
            throw new InvalidOperationException($"Could not find the card with given id: {cardId}");
        }

        if (Card.GetNumber(TopDiscardCardId) is null)
        {
            throw new InvalidOperationException($"Discard pile is empty");
        }

        ToDiscard((CardValue)card?.Number!);
        card?.ChangeNumber((CardValue)Card.GetNumber(TopDiscardCardId)!);
    }

    /// <summary>
    /// Needs to assign the current value of the held card (it must have one) to the top of the discard and null the drawn card
    /// </summary>
    private void SwapBothPlaceholders()
    {
        CardValue? valueToDiscard = Card.GetNumber(HeldCardId);
        Debug.Assert(valueToDiscard != null, "Can't discard a placeholder with no value!");
        Card.ChangePlaceholderNumber(HeldCardId, null);
        _discardBuffer.Push((CardValue)valueToDiscard);
    }

    /// <summary>
    /// Change two card values that are associated with <see cref="Card"/>s that are both in hands
    /// </summary>
    /// <param name="cardOneId"> id of the first card, must be in a hand </param>
    /// <param name="cardTwoId"> id of the second card, must be in a hand</param>
    /// <exception cref="InvalidOperationException"> Thrown if the <see cref="Card"/>s can't be found </exception>
    private void SwapNoPlaceholders(ushort cardOneId, ushort cardTwoId)
    {
        Card? card1 = null;
        Card? card2 = null;

        foreach (List<Card> hand in _playerHands.Values)
        {
            // have to do this strangeness because FirstOrDefault makes a Card instead of a null
            if (hand.Any(card => card.Id == cardOneId))
            {
                card1 = hand.First(card => card.Id == cardOneId);
            }

            if (hand.Any(card => card.Id == cardTwoId))
            {
                card2 = hand.First(card => card.Id == cardTwoId);
            }
        }

        if (card1 is null || card2 is null)
        {
            throw new InvalidOperationException($"Could not find one or both of the cards with given ids: {cardOneId}, {cardTwoId}");
        }

        var value1 = (CardValue)card1?.Number!;
        var value2 = (CardValue)card2?.Number!;
        card1?.ChangeNumber(value2);
        card2?.ChangeNumber(value1);
    }
    #endregion

    /// <summary>
    /// Causes every stored <see cref="CardValue"/> in a single hand to be randomly swapped with another from the same hand
    /// For example, if a hand has 3 cards : { Id: 13 , Value: 2 of diamonds }, { 14, 3 of hearts }, { 18, Ace of spades }
    /// After <see cref="Scramble(byte)"/> the result may be : { 13, Ace of spades }, { 14, 3 of hearts }, { 18, 2 of diamonds }
    /// </summary>
    /// <param name="playerId"> The id of the player who's hand should be scrambled. </param>
    /// <exception cref="ArgumentException"> If the given player hand is not found, exception thrown. </exception>
    public void Scramble(byte playerId)
    {
        if (!_playerHands.TryGetValue(playerId, out List<Card>? hand))
        {
            throw new ArgumentException($"Player hand with id: {playerId} could not be found");
        }

        List<CardValue> cardValues = [];
        hand.ForEach(card => cardValues.Add((CardValue)card.Number!));
        var r = new Random();
        cardValues = [.. cardValues.OrderBy(x => r.NextDouble())];

        for (var i = 0; i < cardValues.Count; i++)
        {
            hand[i].ChangeNumber(cardValues[i]);
        }
    }

    /// <summary>
    /// If the value of the given card is equal to the last discarded card, the given card will be discarded
    /// From the player who's hand it is in.
    /// Otherwise, no removal of that card from the player's hand occurs (nor any discard)
    /// </summary>
    /// <param name="cardId"> The id of the card to attempt to discard. </param>
    /// <returns> Whether the placement was successful. </returns>
    /// <exception cref="ArgumentException"> If the given card id is not found, exception thrown. </exception>
    public bool QuickPlace(ushort cardId)
    {
        List<Card>? hand = null;
        Card? cardToPlace = null;

        foreach (KeyValuePair<byte, List<Card>> searchHand in _playerHands)
        {
            List<Card> consideredHand = searchHand.Value;
            consideredHand.ForEach(card =>
            {
                if (card.Id == cardId)
                {
                    cardToPlace = card;
                }
            });

            if (cardToPlace is not null)
            {
                hand = searchHand.Value;
                break;
            }
        }

        if (hand is null)
        {
            throw new ArgumentException($"Requested card with id: {cardId} could not be found");
        }

        if (Card.GetNumber(TopDiscardCardId) != cardToPlace?.Number)
        {
            return false;
        }

        ToDiscard((CardValue)cardToPlace?.Number!);
        return hand.Remove((Card)cardToPlace!);
    }

    /// <summary>
    /// Causes a card to be added to the specified hand. Typically in response to a failed QuickPlace
    /// </summary>
    /// <param name="handId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"> Thrown if the given hand id can't be found </exception>
    /// <exception cref="InvalidOperationException"> Thrown if the deck has no cards left </exception>
    public ushort PunishPlayer(byte handId)
    {
        if (!_playerHands.TryGetValue(handId, out List<Card>? hand))
        {
            throw new ArgumentException($"Requested hand with id: {handId} could not be found");
        }

        hand?.Add(new(Deck.Draw()));

        return (ushort)hand?.Last().Id!;
    }

    /// <summary>
    /// Sums the value of every card in every hand, according to Eliminator rules:
    /// Non-face cards are worth their number values
    /// Ace is worth 1, J/Q are worth 11/12 respectively
    /// Clubs and Spades K are worth 0, Hearts and Diamonds K are worth 13
    /// Jokers are worth -2
    /// LOW score is better
    /// </summary>
    /// <returns> A list of player id and the sum value of cards in their hand. </returns>
    public List<(byte, int)> CalculateHandValues()
    {
        var playerHandValues = new List<(byte, int)>();
        foreach (KeyValuePair<byte, List<Card>> hand in _playerHands)
        {
            var handSum = 0;
            hand.Value.ToList().ForEach(card =>
            {
                var underlyingValue = (byte)card.Number!;
                if (underlyingValue > 52) // Jokers take away 2
                {
                    handSum -= 2;
                }
                else if (underlyingValue is 26 or 39) // Red Kings add 13, Black Kings add 0
                {
                    handSum += 13;
                }
                else
                {
                    handSum += underlyingValue % 13;
                }
            });

            playerHandValues.Add((hand.Key, handSum));
        }

        return playerHandValues;
    }
}
