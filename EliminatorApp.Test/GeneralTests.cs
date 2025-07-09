namespace EliminatorApp.Test;
public class GeneralTests
{
    [Fact]
    public void FixedCardIsNullable() // Getting a strange compiler warning so thought I'd check this
    {
        FixedCard? nullCard = null;
        Assert.Null(nullCard);
    }
}
