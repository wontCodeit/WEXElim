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
                return GetCardsFromQP(userId, currentPlayerId);

            case GameState.TurnStart: // May transition to QP
                IEnumerable<ushort> fromQuickPlace = GetCardsFromQP(userId, currentPlayerId);
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

    /// <summary>
    /// Check if quick placing is allowed
    /// </summary>
    /// <param name="currentState"> The <see cref="GameState"/> that you are querying for </param>
    /// <param name="userId"> The id of the player you are querying for i.e. the person who might quick place </param>
    /// <param name="currentPlayerId"> The id of the turn player i.e. whose turn it is </param>
    /// <returns> Whether the specified user, for the specified game state, can make a QuickPlace </returns>
    public bool CheckCanQuickPlace(GameState currentState, byte userId, byte currentPlayerId)
    {
        return _handManager.RemainingCards > 0
            && (currentState == GameState.TurnStart || currentState == GameState.TurnEnd || currentState == GameState.Waiting || currentState == GameState.QuickPlace)
            && !UserIsLocked(userId, currentPlayerId);
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

    /// <summary>
    /// Check if a player can currently Call. Calling it must end their turn.
    /// </summary>
    /// <param name="currentState"></param>
    /// <returns></returns>
    public bool CheckCanCall(GameState currentState)
    {
        return _isCalled ? false : currentState == GameState.TurnEnd;
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

        try
        {
            List<byte> playersInNewOrder = [_callItPlayerId!.Value];
            for (var i = 1; i < playerIds.Count; i++)
            {
                playersInNewOrder.Add(GetNextPlayerId(playersInNewOrder[-1]));
            }

            var indexOfCurrentPlayer = playersInNewOrder.FindIndex(id => id == currentPlayerId);

            return playersInNewOrder.Slice(indexOfCurrentPlayer, playerIds.Count);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return []; // No player is valid to start- expect a GameEnd packet from server shortly
        }
    }

    /// <summary>
    /// Assumes correct state. Returns nothing if QP is not valid, if it is returns cards in hand
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="currentPlayerId"></param>
    /// <returns></returns>
    private IEnumerable<ushort> GetCardsFromQP(byte userId, byte currentPlayerId)
    {
        return UserIsLocked(userId, currentPlayerId) || _handManager.RemainingCards == 0
                    ? []
                    : GetCardsInHand(userId).Select(card => card.Id);
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
