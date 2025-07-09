using System.Diagnostics;

namespace Eliminator;
public class Deck: IDeck
{
    private readonly Stack<CardValue> _deck = [];

    public int StandardSizeMultiple { get; private set; }

    public int Remaining => _deck.Count;

    /// <summary>
    /// Instantiates a <see cref="Deck"/>.
    /// </summary>
    /// <param name="amount"> The number of complete sets of 54 cards the <see cref="Deck"/> should contain. </param>
    public Deck(int amount)
    {
        Debug.Assert(amount > 0, "Cannot make a deck with negative or 0 cards in it.");
        StandardSizeMultiple = amount;
        IEnumerable<CardValue> enums = Enum.GetValues(typeof(CardValue)).Cast<CardValue>();
        for (var i = 0; i < amount; i++)
        {
            var max = (byte)enums.Max();
            for (var j = 1; j <= max; j++)
            {
                _deck.Push(enums.ElementAt(j));
            }

            var r = new Random();
            _deck = new(_deck.OrderBy(x => r.NextDouble()));
        }
    }

    /// <summary>
    /// Draw the next <see cref="CardValue"/> from the deck
    /// </summary>
    /// <returns> The drawn <see cref="CardValue"/></returns>
    /// <exception cref="InvalidOperationException"> Thrown when the deck has no cards left </exception>
    public CardValue Draw() => _deck.Pop();
}
