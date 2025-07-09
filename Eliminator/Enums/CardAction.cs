namespace Eliminator;

/// <summary>
/// Actions that can be made by players that effect cards
/// </summary>
public enum CardAction
{
    None,
    Draw,
    PeekSelf,
    PeekOther,
    Swap,
    DiscardSwap, // Swap that is made instead of drawing that can only target the player's own hand and the top of the discard pile
    Scramble,
    QuickPlace
}
