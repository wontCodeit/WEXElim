namespace Eliminator;
public class BlankDeck: IDeck
{
    public int StandardSizeMultiple { get; }

    public int Remaining { get; private set; }

    public BlankDeck(int amount)
    {
        StandardSizeMultiple = amount;
        Remaining = amount * 54;
    }

    public CardValue Draw()
    {
        if (Remaining == 0)
        {
            throw new InvalidOperationException("Can't draw. Out of cards in deck");
        }

        Remaining--;
        return CardValue.Back;
    }
}
