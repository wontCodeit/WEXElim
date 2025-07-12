using Eliminator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EliminatorApp;

/// <summary>
/// A small class that carries a const-like reference to a <see cref="HandManager"/> and a few key variables
/// used to ascertain what inputs are valid for the user to make
/// </summary>
public class InputValidator
{
    private readonly HandManager _handManager;

    private bool _isCalled = false;
    private byte? _callItPlayerId;

    public InputValidator(HandManager handManager, byte? callingPlayerId = null)
    {
        _handManager = handManager;
        if (callingPlayerId != null)
        {
            OnCalledIt((byte)callingPlayerId);
        }
    }

    /// <summary>
    /// Gives all <see cref="Card"/> ids that are valid for the user to select. NOTE: see <see cref="CheckCanDraw"/> for the deck card
    /// Finishing a QuickPlace that has been started is always valid
    /// </summary>
    /// <param name="currentState"> Current state of the game </param>
    /// <param name="userId"> The id of the player to get valid <see cref="Card"/>s for </param>
    /// <param name="currentPlayerId"> The id of the player who's turn it currently is, NOT the player to get valid cards for </param>
    /// <returns> An <see cref="IEnumerable{ushort}"/> of card ids which are valid for the user to select </returns>
    public IEnumerable<ushort> GetValidCardIds(GameState currentState, byte userId, byte currentPlayerId)
    {
        switch (currentState)
        {
            case GameState.Initialisation:
            case GameState.QuickPlace: // No card is valid, only the top of deck button
            case GameState.Scramble: // No card is valid, only hands
                return [];

            case GameState.Waiting: // Transitions to QP
            case GameState.TurnEnd: // Transitions to QP
                return UserIsLocked(userId, currentPlayerId)
                    ? []
                    : GetCardsInHand(userId).Select(card => card.Id);

            case GameState.TurnStart: // May transition to QP
                IEnumerable<ushort> fromQuickPlace = UserIsLocked(userId, currentPlayerId)
                    ? []
                    : GetCardsInHand(userId).Select(card => card.Id);
                return fromQuickPlace.Append(_handManager.TopDiscardCardId);

            case GameState.DeckDraw: // Swap with any card in hand or discard immediately
                IEnumerable<ushort> handCards = GetCardsInHand(userId).Select(card => card.Id);
                return handCards.Append(_handManager.TopDiscardCardId);

            case GameState.DiscardSwap: // When drawing from discard, only swaps with in-hand cards
            case GameState.PeekSelf:
                return GetCardsInHand(userId).Select(card => card.Id);

            case GameState.PeekOther:
                List<ushort> cardsInOtherHands = [];
                IEnumerable<byte> otherPlayerIds = GetNonLockedPlayerIds(currentPlayerId).Where(id => id != userId);
                foreach (var id in otherPlayerIds)
                {
                    cardsInOtherHands.AddRange(GetCardsInHand(id).Select(card => card.Id));
                }

                return cardsInOtherHands;

            case GameState.SwapCardInHands: // TODO: Check if GNLP is excluding the current player
                List<ushort> validSelections = [];
                IEnumerable<byte> nonLockedIds = GetNonLockedPlayerIds(currentPlayerId);
                nonLockedIds.ToList().ForEach(playerId =>
                {
                    validSelections.AddRange(_handManager.GetCardsInHand(playerId).Select(card => card.Id));
                });
                return validSelections;

            default:
                throw new NotImplementedException("Given GameState into GetValidCardIds has no implemented case");
        }
    }

    public void OnCalledIt(byte callingPlayerId)
    {
        _isCalled = true;
        _callItPlayerId = callingPlayerId;
    }

    public bool CheckCanDraw(GameState currentState) => (currentState == GameState.TurnStart) && (_handManager.RemainingCards > 0);
    public bool CheckCanPass(GameState currentState)
    {
        switch (currentState)
        {
            case GameState.TurnStart:
            case GameState.TurnEnd:
                return true;
            default:
                return false;
        }
    }

    public bool CheckCanCall(GameState currentState)
    {
        if (_isCalled)
        {
            return false;
        }

        switch (currentState)
        {
            case GameState.Initialisation:
            case GameState.Waiting:
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Checks whether the user's hand is locked as per the rules of "Calling it" in Eliminator
    /// </summary>
    /// <param name="userId"> The id of the player to check the hand of </param>
    /// <param name="currentPlayerId"> The id of the player who's turn it currently is </param>
    /// <returns> True if the user's hand is locked, False otherwise </returns>
    public bool UserIsLocked(byte userId, byte currentPlayerId) => !GetNonLockedPlayerIds(currentPlayerId).Contains(userId);

    /// <summary>
    /// Get the id of every player who's hand isn't locked
    /// </summary>
    /// <param name="currentPlayerId"> The player who's turn it currently is </param>
    /// <returns> <see cref="IEnumerable{byte}"/> of player ids </returns>
    public IEnumerable<byte> GetNonLockedPlayerIds(byte currentPlayerId)
    {
        List<byte> playerIds = _handManager.PlayerIds();

        if (!_isCalled)
        {
            return playerIds;
        }

        List<byte> playersInNewOrder = [_callItPlayerId!.Value];
        for (var i = 1; i < playerIds.Count; i++)
        {
            playersInNewOrder.Add(GetNextPlayerId(playersInNewOrder[-1]));
        }

        var indexOfCurrentPlayer = playersInNewOrder.FindIndex(id => id == currentPlayerId);
        try
        {
            return playersInNewOrder.Slice(indexOfCurrentPlayer, playerIds.Count);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return [];
        }
    }

    private List<ICard> GetCardsInHand(byte userId) => _handManager.GetCardsInHand(userId);

    private byte GetNextPlayerId(byte currentPlayerId)
    {
        List<byte> playerIds = _handManager.PlayerIds();

        return playerIds.Contains((byte)(currentPlayerId + 1))
            ? (byte)(currentPlayerId + 1)
            : playerIds[0];
    }
}
