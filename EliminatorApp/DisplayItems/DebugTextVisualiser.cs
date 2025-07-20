using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

using System.Linq;

namespace EliminatorApp;

/// <summary>
/// Class for displaying non-obvious changes of game state to the user, useful in debugging. Hopefully can be used in something in release
/// </summary>
public static class DebugTextVisualiser
{
    private static List<DrawMeText> _textToDraw = [];

    /// <summary>
    /// Create a new text instance to draw. Uses default colour and duration
    /// </summary>
    /// <param name="drawMeText"> The text to draw </param>
    public static void AddDrawMeText(string drawMeText)
    {
        _textToDraw.Add(new(drawMeText));
    }

    /// <summary>
    /// Create a new text instance to draw.
    /// </summary>
    /// <param name="drawMeText"> The text to draw </param>
    /// <param name="colour"> Colour that the text will be drawn as </param>
    /// <param name="lifetime"> Lifetime in frames of the text on screen </param>
    public static void AddDrawMeText(string drawMeText, Color colour, int lifetime)
    {
        _textToDraw.Add(new(drawMeText, colour, lifetime));
    }

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
        while (checkIfTextFits())
        {
            scaling *= 0.9;
        }

        // Now we know how many DrawMeText objects can fit side by side and that they will all fit in the space
        // They can all be drawn in the render target
        Vector2 currentPosition = Vector2.Zero;
        var textObjsPlacedInRow = 0;
        for (var i = 0; i < _textToDraw.Count; i++)
        {
            _textToDraw[i].RemainingLifetime--;
            spriteBatch.DrawString(
                font,
                _textToDraw[i].Text,
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

            currentPosition = new Vector2(0, currentPosition.Y + font.MeasureString(_textToDraw[i].Text).Y);
            textObjsPlacedInRow = 0;
        }

        _textToDraw = [.. _textToDraw.Where(drawMe => drawMe.RemainingLifetime > 0)];
    }

    private class DrawMeText
    {
        public string Text { get; } = "Not set";
        public Color Colour { get; } = Color.White;
        public int RemainingLifetime { get; set; } = 360;

        /// <summary>
        /// Construct an instance
        /// </summary>
        /// <param name="text"> The text that shall be drawn </param>
        /// <param name="colour"> Colour of the text that will be drawn </param>
        public DrawMeText(string text, Color colour, int lifetime)
        {
            Text = text;
            Colour = colour;
            RemainingLifetime = lifetime;
        }

        /// <summary>
        /// Create instance with a white colour (default)
        /// </summary>
        /// <param name="text"></param>
        public DrawMeText(string text)
        {
            Text = text;
        }
    }
}
