namespace EliminatorApp;

/// <summary>
/// Triggers for the <see cref="GameStateMachine"/> TODO: Add an end of game state?
/// </summary>
public enum Trigger
{
    StartTurn,
    SelectionUpdate,
    DeckClick,
    DoCardAction,
    EndTurn,
    CancelAction // For flexibility on moving state. Current implementation expected to be when cards de-selected
}
