namespace Eliminator;
public interface IDeck
{
    public int StandardSizeMultiple { get; }
    public int Remaining { get; }
    public CardValue Draw();

}
