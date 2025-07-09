using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliminatorApp;
public interface IButton
{
    /// <summary>
    /// Identifier of this button
    /// </summary>
    public ButtonId ButtonId { get; }

    /// <summary>
    /// Whether this <see cref="IButton"/> is currently valid for clicking
    /// </summary>
    public bool Clickable { get; set; }

    /// <summary>
    /// Check whether a point lies within this <see cref="IButton"/>'s area
    /// </summary>
    /// <param name="point"> The point to test against </param>
    /// <param name="scale"> The scale of this instance within the containing <see cref="RenderTarget2D"/> </param>
    /// <returns> Whether the point intersects with this <see cref="IButton"/></returns>
    public bool CheckIntersection(Vector2 point, float scale = 1.0f);

    /// <summary>
    /// Do some action
    /// </summary>
    public void Click();
}
