using Eliminator;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EliminatorApp;

/// <summary>
/// Class that handles <see cref="GameState"/>
/// performs transitions and notifies observers of these transitions (where upon user validation needs updating)
/// </summary>
public class GameStateMachine
{
    private readonly StateMachine<GameState, Trigger> _stateMachine;
    private readonly StateMachine<GameState, Trigger>.TriggerWithParameters<byte> _startTurnTrigger;
    private readonly StateMachine<GameState, Trigger>.TriggerWithParameters<IEnumerable<IButton>> _selectionUpdateTrigger;
    private readonly StateMachine<GameState, Trigger>.TriggerWithParameters<CardAction> _doCardActionTrigger;

    private readonly HandManager _handManager; // const-like ref, simply contains all data needed for checking dynamic state transitions/guards
    private readonly byte _userId;

    // Tracks the state that quick place was entered from (history)
    private GameState? _quickPlaceEntryPoint = null;

    public GameState CurrentState => _stateMachine.State;

    public IEnumerable<Trigger> ValidTriggers => _stateMachine.GetPermittedTriggers();

    public event EventHandler<GameState> StateChanged;

    /// <summary>
    /// Construct a  <see cref="GameStateMachine"/> which configures a <see cref="StateMachine{GameState, Trigger}"/>
    /// </summary>
    /// <param name="handManager"></param>
    /// <param name="userId"></param>
    public GameStateMachine(HandManager handManager, byte userId)
    {
        _handManager = handManager;
        _userId = userId;

        _stateMachine = new(GameState.Initialisation);
        _startTurnTrigger = _stateMachine.SetTriggerParameters<byte>(Trigger.StartTurn);
        _selectionUpdateTrigger = _stateMachine.SetTriggerParameters<IEnumerable<IButton>>(Trigger.SelectionUpdate);
        _doCardActionTrigger = _stateMachine.SetTriggerParameters<CardAction>(Trigger.DoCardAction);

        _stateMachine.OnTransitionCompleted((_) => StateChanged?.Invoke(this, CurrentState));

        // N.B. all discards are of a config object
        _ = _stateMachine.Configure(GameState.Initialisation)
            .PermitIf(_startTurnTrigger, GameState.TurnStart, startId => startId == _userId)
            .PermitIf(_startTurnTrigger, GameState.Waiting, startId => startId != _userId);

        _ = _stateMachine.Configure(GameState.Waiting)
            .PermitIf(_startTurnTrigger, GameState.TurnStart, startId => startId == _userId)
            .PermitIf(_selectionUpdateTrigger, GameState.QuickPlace, IsQuickPlace)
            .OnExit(TrackQuickPlace);

        _ = _stateMachine.Configure(GameState.QuickPlace)
            .PermitDynamic(Trigger.CancelAction, QuickPlaceFinished)
            .PermitDynamic(_doCardActionTrigger, CardActionToState);

        _ = _stateMachine.Configure(GameState.TurnStart)
            .Permit(Trigger.EndTurn, GameState.Waiting)
            .PermitIf(Trigger.DeckClick, GameState.DeckDraw, () => _handManager.RemainingCards > 0)
            .PermitIf(_selectionUpdateTrigger, GameState.DiscardSwap, SelectedCardIsDiscardCard)
            .PermitIf(_selectionUpdateTrigger, GameState.QuickPlace, IsQuickPlace)
            .OnExit(TrackQuickPlace);

        _ = _stateMachine.Configure(GameState.DeckDraw)
            .PermitDynamic(_doCardActionTrigger, CardActionToState);

        _ = _stateMachine.Configure(GameState.DiscardSwap)
            .Permit(Trigger.CancelAction, GameState.TurnStart)
            .PermitDynamic(_doCardActionTrigger, CardActionToState);

        _ = _stateMachine.Configure(GameState.TurnEnd)
            .Permit(Trigger.EndTurn, GameState.Waiting)
            .PermitIf(_selectionUpdateTrigger, GameState.QuickPlace, IsQuickPlace)
            .OnExit(TrackQuickPlace);

        _ = _stateMachine.Configure(GameState.PeekSelf)
            .Permit(Trigger.CancelAction, GameState.TurnEnd)
            .PermitDynamicIf(_selectionUpdateTrigger, ExitStateFromCardAction, CardsAreInHand);

        _ = _stateMachine.Configure(GameState.PeekOther)
            .Permit(Trigger.CancelAction, GameState.TurnEnd)
            .PermitDynamicIf(_selectionUpdateTrigger, ExitStateFromCardAction, CardsAreInOtherHand);

        _ = _stateMachine.Configure(GameState.SwapCardInHands)
            .Permit(Trigger.CancelAction, GameState.TurnEnd)
            .PermitDynamicIf(_selectionUpdateTrigger,
                             ExitStateFromCardAction,
                             inputs => inputs.Count() == 2 && (CardsAreInHand(inputs) || CardsAreInOtherHand(inputs)));

        _ = _stateMachine.Configure(GameState.Scramble)
            .Permit(Trigger.CancelAction, GameState.TurnEnd)
            .PermitDynamicIf(_selectionUpdateTrigger, ExitStateFromCardAction, InputIsHand);
    }

    // Defining some complex generic to fire parametrised triggers seems silly/over-engineering
    // I also decided to make Trigger(s) not directly fire-able so no parametrised Triggers are accidentally missed
    #region PublicTriggerFiring
    public void FireStartTurnTrigger(byte startPlayerId) => _stateMachine.Fire(_startTurnTrigger, startPlayerId);

    public void FireSelectionUpdateTrigger(IEnumerable<IButton> inputRegistry) => _stateMachine.Fire(_selectionUpdateTrigger, inputRegistry);

    public void FireDoCardActionTrigger(CardAction cardAction) => _stateMachine.Fire(_doCardActionTrigger, cardAction);

    public void FireDeckClickTrigger() => _stateMachine.Fire(Trigger.DeckClick);

    public void FireEndTurnTrigger() => _stateMachine.Fire(Trigger.EndTurn);

    public void FireCancelActionTrigger() => _stateMachine.Fire(Trigger.CancelAction);
    #endregion

    /// <summary>
    /// Check if <see cref="Trigger.CancelAction"/> is valid in the current state.
    /// The in-built check is bad because it passes null params which breaks the guard clauses
    /// </summary>
    /// <returns> Whether or not <see cref="Trigger.CancelAction"/> is a valid trigger </returns>
    public bool CanFireCancelTrigger()
    {
        switch (CurrentState)
        {
            case GameState.QuickPlace:
            case GameState.DiscardSwap:
            case GameState.PeekSelf:
            case GameState.PeekOther:
            case GameState.SwapCardInHands:
            case GameState.Scramble:
                return true;
            default:
                return false;
        }
    }

    private bool InputIsHand(IEnumerable<IButton> inputs) => inputs.Count() == 1 && inputs.First() is HandView;

    private bool CardsAreInOtherHand(IEnumerable<IButton> inputs)
    {
        if (inputs.Count() != 1 || inputs.First() is not FixedCard card)
        {
            return false;
        }

        foreach (var handId in _handManager.PlayerIds().Where(hand => hand != _userId))
        {
            if (_handManager.GetCardsInHand(handId).Select(card => card.Id).Contains(card.RepresentedCard.Id))
            {
                return true;
            }
        }

        return false;
    }

    private GameState ExitStateFromCardAction(IEnumerable<IButton> inputs)
    {
        return _quickPlaceEntryPoint is null
            ? GameState.TurnEnd
            : (GameState)_quickPlaceEntryPoint;
    }

    private bool SelectedCardIsDiscardCard(IEnumerable<IButton> inputReg)
    {
        return inputReg.Any()
            && inputReg.First() is FixedCard card
            && _handManager.TopDiscardCardId == ((ushort)card.ButtonId.Value);
    }

    private GameState CardActionToState(CardAction cardAction)
    {
        switch (cardAction)
        {
            case CardAction.None:
                return QuickPlaceFinished();
            case CardAction.Swap:
                return GameState.SwapCardInHands;
            case CardAction.PeekSelf:
                return GameState.PeekSelf;
            case CardAction.PeekOther:
                return GameState.PeekOther;
            case CardAction.Scramble:
                return GameState.Scramble;
            default:
                throw new NotImplementedException("Unaccounted for card action given OR card action was not valid in this context");
        }
    }

    private void TrackQuickPlace(StateMachine<GameState, Trigger>.Transition transition)
    {
        _quickPlaceEntryPoint = transition.Destination == GameState.QuickPlace
            ? transition.Source
            : null;
    }

    private GameState QuickPlaceFinished()
    {
        var state = (GameState)_quickPlaceEntryPoint!;
        _quickPlaceEntryPoint = null;
        return state;
    }

    /// <summary>
    /// Checks whether quick placing is valid/is selected. NOTE: Does not account for locking/calling! That is the purview of input validator.
    /// </summary>
    /// <param name="inputReg"> All currently selected buttons. Should only contain cards. </param>
    /// <returns> Whether quick place can be transitioned to </returns>
    private bool IsQuickPlace(IEnumerable<IButton> inputReg) =>
        inputReg.Count() == 1 && _handManager.RemainingCards > 0 && CardsAreInHand(inputReg);

    private bool CardsAreInHand(IEnumerable<IButton> inputReg)
    {
        var cardsInHand = _handManager.GetCardsInHand(_userId).Select(card => card.Id).ToList();

        foreach (IButton input in inputReg)
        {
            if (input as FixedCard is null)
            {
                continue;
            }

            if (cardsInHand.Contains((ushort)input.ButtonId.Value))
            {
                return true;
            }
        }

        return false;
    }
}
