namespace EliminatorApp;

/// <summary>
/// For easy creation of ids for buttons. Begins at 1 beyond what a card id can be.
/// TODO: Make this thread safe and easily reset, it has the same problems as <see cref="Eliminator.Card"/>
/// </summary>
public struct ButtonId
{
    private static int _nextId = ushort.MaxValue + 1;
    public int Value;

    /// <summary>
    /// Create a new <see cref="ButtonId"/>
    /// </summary>
    public ButtonId()
    {
        Value = _nextId++;
    }

    /// <summary>
    /// Create a <see cref="ButtonId"/> that is the same as a card id
    /// </summary>
    /// <param name="cardId"></param>
    public ButtonId(ushort cardId)
    {
        Value = cardId;
    }
}
