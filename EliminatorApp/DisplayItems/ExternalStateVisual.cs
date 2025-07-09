using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace EliminatorApp;

public struct DrawMeText
{
    public string Text { get; }
    public Color Color { get; }
    public int RemainingLifeTime { get; private set; }

    // Destroy this when it's life time hits zero
    public bool DecrementLifeTime()
    {
        RemainingLifeTime--;
        return RemainingLifeTime > 0;
    }
}

/// <summary>
/// Class for displaying non-obvious changes of game state to the user, useful in debugging
/// </summary>
public static class ExternalStateVisual
{
    public static int FrameCounter { get; private set; }

    public static List<DrawMeText> TextToDraw { get; private set; } = [];

    public static void AddDrawMeText(DrawMeText drawMeText)
    {
        TextToDraw.Add(drawMeText);
    }
}
