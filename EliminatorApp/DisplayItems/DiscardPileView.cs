using Eliminator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EliminatorApp;
public class DiscardPileView: IView, IButton
{
    private readonly EventHandler _discardClickedEvent;
    private bool disposedValue;

    public CardValue DisplayedDiscardValue { get; set; }

    public RenderTarget2D View { get; }

    public DisplaySpace DisplaySpace { get; }

    public ButtonId ButtonId { get; }

    public bool Clickable { get; set; }

    public DiscardPileView(
                    CardValue initialDiscard,
                    EventHandler discardClickedEvent,
                    ButtonId deckButtonID,
                    RenderTarget2D view,
                    DisplaySpace space)
    {
        DisplayedDiscardValue = initialDiscard;
        _discardClickedEvent = discardClickedEvent;
        ButtonId = deckButtonID;
        View = view;
        DisplaySpace = space;
    }

    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        graphicsDevice.SetRenderTarget(View);
        graphicsDevice.Clear(Color.Gray);

        spriteBatch.Begin();
        spriteBatch.Draw(
            Game1.CardTextures[DisplayedDiscardValue],
            new(),
            null,
            Color.White,
            0f,
            new(),
            1.0f,
            SpriteEffects.None,
            0f);

        spriteBatch.End();
    }

    public bool CheckIntersection(Vector2 point, float scale = 1)
    {
        // TODO: We should not be making these rectangles all the time, instead we should:
        // check if we have one already created
        // if so, check if it is the correct scale
        // if either of the above are false, create a new one and store it
        // return if point is contained.
        // same is true in DeckView, and perhaps elsewhere too.
        // on the other hand, it is very quick to create new rectangles
        // and since we avoid two additional branches, we might actually be quicker doing it this way
        // TODO: Check if it would be more performant to store the rect rather than remake it every time

        Rectangle rect = new((int)DisplaySpace.Position.X,
                             (int)DisplaySpace.Position.Y,
                             (int)(Game1.CARD_WIDTH * scale) + 1,
                             (int)(Game1.CARD_HEIGHT * scale) + 1);

        return rect.Contains(point);
    }

    public void Click()
    {
        _discardClickedEvent?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                View.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~DiscardPileView()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
