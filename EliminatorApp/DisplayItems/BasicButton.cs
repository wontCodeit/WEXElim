using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EliminatorApp;

public class BasicButton: IButton, IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly EventHandler _clickedEvent;
    private bool disposedValue;

    public ButtonId ButtonId { get; }

    public bool Clickable { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public DisplaySpace DisplaySpace { get; }

    public Texture2D Texture { get; }

    public BasicButton(DisplaySpace displaySpace, Texture2D texture, EventHandler clickedEvent)
    {
        ButtonId = new ButtonId();
        DisplaySpace = displaySpace;
        Texture = texture;
        _width = texture.Width;
        _height = texture.Height;
        _clickedEvent = clickedEvent;
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
        _clickedEvent?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                Texture.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~BasicButton()
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
