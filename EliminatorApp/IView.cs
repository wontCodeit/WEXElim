using Microsoft.Xna.Framework.Graphics;
using System;

namespace EliminatorApp;
public interface IView: IDisposable
{
    public RenderTarget2D View { get; }
    public DisplaySpace DisplaySpace { get; }
    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch);
}
