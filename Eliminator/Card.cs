using System.Diagnostics;

namespace Eliminator;

/// <summary>
/// Contains an Id which is tied to a value. Values may change, ids (or cards) should not move if they don't have to
/// TODO: this thing needs resetting and I worry about it's global state being hard to manage, perhaps there is a better solution
/// Destruction of all Card instances does not reset Id count nor stored values
/// </summary>
public struct Card
{
    private static ushort _nextId = 0;
    private static readonly List<ushort> _placeholders = [];
    public ushort Id { get; }

    /// <summary>
    /// Gets the value of this card. If it is null, this card is the Drawn Card or the Last Discarded Card as these are a sort of placeholder
    /// that need ids for interaction with the Swap action however they do not always have values
    /// </summary>
    /// <exception cref="InvalidOperationException"> Thrown when the card has no given value </exception>
    public CardValue? Number => GetNumber();
    public Card(CardValue value)
    {
        Id = _nextId++;
        _numbers.Add(Id, value);
    }

    /// <summary>
    /// Set next id to 0 and erase all stored data. Useful for running multiple Eliminator games and unit tests
    /// </summary>
    public static void Reset()
    {
        _nextId = 0;
        _placeholders.Clear();
        _numbers.Clear();
    }

    /// <summary>
    /// Add an entry to the underlying <see cref="Card"/> dictionary that can have a null value
    /// </summary>
    /// <returns> The id of the placeholder </returns>
    public static ushort AddPlaceholder()
    {
        _placeholders.Add(_nextId);
        _numbers.Add(_nextId, null);
        return _nextId++;
    }

    /// <summary>
    /// Change the <see cref="CardValue"/> stored in a placeholder card only
    /// </summary>
    /// <param name="placeholderId"> id of the placeholder </param>
    /// <param name="value"> <see cref="CardValue"/> to assign </param>
    public static void ChangePlaceholderNumber(ushort placeholderId, CardValue? value)
    {
        Debug.Assert(
            _placeholders.Contains(placeholderId),
            "Can't give a non-placeholder Card a possibly null value");

        _numbers[placeholderId] = value;
    }

    public static CardValue? GetNumber(ushort cardId)
    {
        var success = _numbers.TryGetValue(cardId, out CardValue? cardValue);
        return success
            ? cardValue
            : throw new InvalidOperationException("Card has no value");
    }

    public void ChangeNumber(CardValue value) // ignore the readonly, it doesn't know what's up
    {
        _numbers[Id] = value;
    }

    private CardValue? GetNumber()
    {
        var success = _numbers.TryGetValue(Id, out CardValue? cardValue);
        return success
            ? cardValue
            : throw new InvalidOperationException("Card has no value");
    }

    private static readonly Dictionary<ushort, CardValue?> _numbers = [];
}
