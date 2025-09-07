using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EliminatorApp;

public class EndTurnButton: IButton
{
    private readonly int _width;
    private readonly int _height;
    private readonly EventHandler _endButtonEvent;

    public ButtonId ButtonId { get; }

    public bool Clickable { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public DisplaySpace DisplaySpace { get; }

    public Texture2D Texture { get; }

    public EndTurnButton(DisplaySpace displaySpace, Texture2D texture, EventHandler endButtonClickedEvent)
    {
        ButtonId = new ButtonId();
        DisplaySpace = displaySpace;
        Texture = texture;
        _width = texture.Width;
        _height = texture.Height;
        _endButtonEvent = endButtonClickedEvent;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool CheckIntersection(Vector2 point, float scale = 1.0f)
    {
        Rectangle rectangle = new(
            (int)DisplaySpace.Position.X,
            (int)DisplaySpace.Position.Y,
            (int)(_width * scale) + 1,
            (int)(_height * scale) + 1); // for very small scales this breaks down due to rounding errors

        return rectangle.RotatedIntersects(DisplaySpace.Rotation, point);
    }

    public void Click()
    {
        _endButtonEvent?.Invoke(this, EventArgs.Empty);
    }
}
