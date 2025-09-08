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
        var enumsNoCardBack = Enum.GetValues<CardValue>().Cast<CardValue>().Where(cv => cv != CardValue.Back).ToList();
        var standardDeckSize = enumsNoCardBack.Count;

        for (var i = 0; i < amount; i++)
        {
            if (firstCards == null || firstCards.Count == 0)
            {
                enumsNoCardBack.ForEach(_deck.Enqueue);
                continue;
            }

            var limit = Math.Min(firstCards.Count, standardDeckSize);

            // remove from enums, so no duplicates are added later
            List<CardValue> enums = [.. enumsNoCardBack.Except(firstCards.Take(limit))];

            for (var j = 0; j < limit; j++)
            {
                _deck.Enqueue(firstCards[0]);
                firstCards.RemoveAt(0);
            }

            enums.ForEach(_deck.Enqueue);
        }
    }

    public CardValue Draw()
    {
        return _deck.Dequeue();
    }
}
