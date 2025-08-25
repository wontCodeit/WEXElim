using Eliminator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EliminatorApp;
internal class RiggedDeck: IDeck
{
    private readonly Queue<CardValue> _deck = [];

    public int StandardSizeMultiple { get; private set; }

    public int Remaining => _deck.Count;

    /// <summary>
    /// Instantiate a <see cref="RiggedDeck"/>. Cards appear in <see cref="CardValue"/> order, except when firstCards defined
    /// </summary>
    /// <param name="amount"> The number of complete sets of 54 cards the <see cref="RiggedDeck"/> should contain. </param>
    /// <param name="firstCards"> Set the first card drawn from this deck to be the first in <paramref name="firstCards"/> and the second as the second etc. </param>
    public RiggedDeck(int amount, List<CardValue>? firstCards)
    {
        Debug.Assert(amount > 0, "Cannot make a deck with negative or 0 cards in it.");
        StandardSizeMultiple = amount;
        IEnumerable<CardValue> enumsNoCardBack = Enum.GetValues<CardValue>().Cast<CardValue>().Where(cv => cv != CardValue.Back);

        for (var i = 0; i < amount; i++)
        {
            IEnumerable<CardValue> enums = [.. enumsNoCardBack];
            if (firstCards != null && firstCards.Count != 0)
            {
                enums = enums.Except(firstCards.Take(54)); // remove from enums, so no duplicates are added later
                var limit = Math.Min(54, firstCards.Count()); // account for firstCards being longer than one deck
                for (var j = 0; j < limit; j++)
                {
                    _deck.Enqueue(firstCards.First());
                    firstCards.RemoveAt(0);
                }
            }

            for (var j = 0; j < enums.Count(); j++)
            {
                _deck.Enqueue(enums.ElementAt(j));
            }
        }
    }

    public CardValue Draw()
    {
        return _deck.Dequeue();
    }
}
