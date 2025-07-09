using Eliminator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EliminatorApp.DisplayItems;
public class HeldCardView: IButton, IView
{
    public ButtonId ButtonId => throw new NotImplementedException();

    public bool Clickable { get; set; }

    public RenderTarget2D View => throw new NotImplementedException();

    public DisplaySpace DisplaySpace => throw new NotImplementedException();

    public HeldCardView(EventHandler<CardValue?> discardCardEvent)
    {
        discardCardEvent += OnDiscardCard;
    }

    private void OnDiscardCard(object? sender, CardValue? e) => throw new NotImplementedException();
    public bool CheckIntersection(Vector2 point, float scale = 1) => throw new NotImplementedException();
    public void Click() => throw new NotImplementedException();
    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) => throw new NotImplementedException();
}
