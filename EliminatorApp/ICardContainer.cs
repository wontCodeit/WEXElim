using System.Collections.Generic;

namespace EliminatorApp;
public interface ICardContainer: IView
{
    public IEnumerable<FixedCard> DisplayCards { get; }
    public DisplaySpace? NextInternalSpace();

}
