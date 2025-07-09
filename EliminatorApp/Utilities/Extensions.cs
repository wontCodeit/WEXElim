using Microsoft.Xna.Framework;

namespace EliminatorApp;

/// <summary>
/// Additional member functions for existing classes
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Check whether a point would lie in a <see cref="Rectangle"/> if that <see cref="Rectangle"/> were to be rotated by some amount around its centre
    /// </summary>
    /// <param name="rectangle"> The given rectangle </param>
    /// <param name="rotation"> How far the rectangle would be rotated by in radians ABOUT ITS CENTRE </param>
    /// <param name="point"> The (not rotated) point to test against </param>
    /// <returns> Whether the point lies in the rotated <see cref="Rectangle"/> </returns>
    public static bool RotatedIntersects(this Rectangle rectangle, float rotation, Vector2 point)
    {
        var rotatedPoint = Vector2.RotateAround(point, new Vector2(rectangle.Center.X, rectangle.Center.Y), rotation);
        var pointRect = new Rectangle((int)rotatedPoint.X, (int)rotatedPoint.Y, 1, 1);
        return rectangle.Intersects(pointRect);
    }
}