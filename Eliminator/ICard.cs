namespace Eliminator;

/// <summary>
/// Contains an Id which is tied to a value. Values may change, ids (or cards) should not move if they don't have to
/// Construction is tied up in a <see cref="CardCounter"/> to track all data related to <see cref="ICard"/>s
/// </summary>
public interface ICard
{
    public ushort Id { get; }

    /// <summary>
    /// Gets the value of this card. If it is null, this card is the Drawn Card or the Last Discarded Card as these are a sort of placeholder
    /// that need ids for interaction with the Swap action however they do not always have values
    /// </summary>
    /// <exception cref="InvalidOperationException"> Thrown when the card has no given value </exception>
    public CardValue? Number { get; }

    public void ChangeNumber(CardValue value);
}
