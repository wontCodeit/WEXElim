using Microsoft.Xna.Framework.Graphics;

namespace EliminatorApp;
public interface IView
{
    public RenderTarget2D View { get; }
    public DisplaySpace DisplaySpace { get; }
    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch);
}
