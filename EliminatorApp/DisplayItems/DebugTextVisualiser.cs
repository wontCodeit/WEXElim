using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EliminatorApp;

public struct DrawMeText
{
    public string Text { get; }
    public Color Colour { get; }
    public int RemainingLifeTime { get; private set; }

    // 
    /// <summary>
    /// Reduce the lifetime on this object. Destroy this when it's life time hits zero (i.e. remove from "TextToDraw")
    /// </summary>
    /// <returns> True if this object should be destroyed </returns>
    public bool DecrementLifeTime()
    {
        RemainingLifeTime--;
        return RemainingLifeTime <= 0;
    }
}

/// <summary>
/// Class for displaying non-obvious changes of game state to the user, useful in debugging. Hopefully can be used in something in release
/// </summary>
public static class DebugTextVisualiser
{
    private static List<DrawMeText> _textToDraw = [];

    public static void AddDrawMeText(DrawMeText drawMeText) => _textToDraw.Add(drawMeText);

    /// <summary>
    /// Draw all the stored <see cref="DrawMeText"/> objects and handle their lifetimes
    /// </summary>
    /// <param name="spriteBatch"> Sprite batch to draw to. .Begin() should be called, UNLESS scaleMatrix is not null </param>
    /// <param name="font"> Font to draw all text as </param>
    /// <param name="targetView"> View port to draw text to </param>
    /// <param name="scaleMatrix"> Pass as null if <see cref="SpriteBatch"/>.Begin() already called. Otherwise, calls .Begin() with this parameter</param>
    public static void DrawAllText(SpriteBatch spriteBatch, SpriteFont font, RenderTarget2D targetView, Matrix? scaleMatrix = null)
    {
        const int VERTICAL_PADDING = 5;
        const int HORIZONTAL_PADDING = 5;

        if (scaleMatrix != null)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                null,
                null,
                null,
                null,
                null,
                scaleMatrix);
        }

        // Find size of view port and all text objects, and then how to scale text objects to fit.
        var totalY = 0f;
        var maxX = 0f;
        var maxTextsInRow = 1;
        var scaling = 1.0;
        _textToDraw.ForEach(drawMeText =>
        {
            Vector2 textRenderedSize = font.MeasureString(drawMeText.Text);
            totalY += textRenderedSize.Y + VERTICAL_PADDING;
            maxX = Math.Max(maxX, textRenderedSize.X + HORIZONTAL_PADDING);
        });

        Func<bool> checkIfTextFits = () =>
        {
            if ((scaling * maxX * (maxTextsInRow + 1)) < targetView.Width)
            {
                maxTextsInRow++;
            }

            return (scaling * totalY) > (targetView.Height / maxTextsInRow);
        };

        // Adjust scaling until all objects will fit
        while (!checkIfTextFits())
        {
            scaling *= 0.9;
        }

        // Now we know how many DrawMeText objects can fit side by side and that they will all fit in the space
        // They can all be drawn in the render target
        Vector2 currentPosition = Vector2.Zero;
        var textObjsPlacedInRow = 0;
        for (var i = 0; i < _textToDraw.Count; i++)
        {
            var text = _textToDraw[i].Text;
            spriteBatch.DrawString(
                font,
                text,
                currentPosition,
                _textToDraw[i].Colour,
                0f,
                Vector2.Zero,
                (float)scaling,
                SpriteEffects.None,
                1.0f);

            textObjsPlacedInRow++;

            // Update currentPosition - if can fit sideways, do so. Else, move down a row.
            if (textObjsPlacedInRow != maxTextsInRow)
            {
                currentPosition.X += maxX;
                continue;
            }

            currentPosition = new Vector2(0, currentPosition.Y + font.MeasureString(text).Y);
            textObjsPlacedInRow = 0;
        }

        _textToDraw = [.. _textToDraw.Where(drawMe => !drawMe.DecrementLifeTime())];
    }
}
