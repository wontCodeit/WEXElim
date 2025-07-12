using System.Diagnostics;

namespace Eliminator;

/// <summary>
/// Class used in the instantiation of <see cref="Card"/>s and tracking their <see cref="CardValue"/>s
/// </summary>
public class CardCounter
{
    private readonly Dictionary<ushort, CardValue?> _numbers = [];
    private readonly List<ushort> _placeholders = [];

    private ushort _nextId = 0;

    public CardCounter()
    { }

    public ICard MakeNewCard(CardValue value)
    {
        _numbers.Add(_nextId, value);
        return new Card(_nextId++, this);
    }

    public void ChangeNumber(ushort Id, CardValue value)
    {
        _numbers[Id] = value;
    }

    /// <summary>
    /// Change the <see cref="CardValue"/> stored in a placeholder card only
    /// </summary>
    /// <param name="placeholderId"> id of the placeholder </param>
    /// <param name="value"> <see cref="CardValue"/> to assign </param>
    public void ChangePlaceholderNumber(ushort placeholderId, CardValue? value)
    {
        Debug.Assert(
            _placeholders.Contains(placeholderId),
            "Can't give a non-placeholder Card a possibly null value");

        _numbers[placeholderId] = value;
    }

    public CardValue? GetNumber(ushort Id)
    {
        var success = _numbers.TryGetValue(Id, out CardValue? cardValue);
        return success
            ? cardValue
            : throw new InvalidOperationException("Card has no value");
    }

    /// <summary>
    /// Add an entry to the underlying <see cref="ICard"/> dictionary that can have a null value
    /// </summary>
    /// <returns> The id of the placeholder </returns>
    public ushort AddPlaceholder()
    {
        _placeholders.Add(_nextId);
        _numbers.Add(_nextId, null);
        return _nextId++;
    }

    /// <summary>
    /// Private implementation of <see cref="ICard"/>, so construction through <see cref="CardCounter"/> is forced
    /// </summary>
    private readonly record struct Card: ICard
    {
        private readonly CardCounter _counter;

        public ushort Id { get; }

        public CardValue? Number => _counter.GetNumber(Id);

        public Card(ushort id, CardCounter counter)
        {
            Id = id;
            _counter = counter;
        }

        public void ChangeNumber(CardValue value) => _counter.ChangeNumber(Id, value);
    }
}

