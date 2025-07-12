using Eliminator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliminatorApp;

/// <summary>
/// Instances used to display cards and allow user interaction
/// </summary>
public class FixedCard: IButton
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ButtonId ButtonId { get; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Clickable { get; set; } = false;

    /// <summary>
    /// What space this <see cref="FixedCard"/> occupies
    /// </summary>
    public DisplaySpace DisplaySpace { get; set; }

    /// <summary>
    /// The <see cref="Card"/> that this <see cref="FixedCard"/> represents
    /// </summary>
    public ICard RepresentedCard { get; }

    /// <summary>
    /// Set to true to prevent this <see cref="FixedCard"/> being drawn
    /// </summary>
    public bool Hide { get; set; } = false;

    /// <summary>
    /// The <see cref="Texture2D"/> used to draw this <see cref="FixedCard"/>
    /// </summary>
    public Texture2D Texture
    {
        get
        {
            CardValue? cardValue = RepresentedCard.Number;
            return cardValue is null ? Game1.NoTexture : Game1.CardTextures[cardValue.Value];
        }
    }

    /// <summary>
    /// Creates a <see cref="FixedCard"/>
    /// </summary>
    /// <param name="card"> The card that this instance will represent </param>
    /// <param name="displaySpace"> The space this card will initially occupy </param>
    public FixedCard(ICard card, DisplaySpace displaySpace)
    {
        RepresentedCard = card;
        DisplaySpace = displaySpace;
        ButtonId = new(card.Id);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool CheckIntersection(Vector2 point, float scale = 1.0f)
    {
        Rectangle rectangle = new(
            (int)DisplaySpace.Position.X,
            (int)DisplaySpace.Position.Y,
            (int)(Game1.CARD_WIDTH * scale) + 1,
            (int)(Game1.CARD_HEIGHT * scale) + 1); // for very small scales this breaks down due to rounding errors

        return rectangle.RotatedIntersects(DisplaySpace.Rotation, point);
    }

    /// <summary>
    /// Adds this to the <see cref="Game1"/> input registry, or remove it if it is already there
    /// </summary>
    public void Click()
    {
        if (!Game1.InputRegistry.Contains(this))
        {
            Game1.InputRegistry.Add(this);
            Hide = true;
            return;
        }

        _ = Game1.InputRegistry.Remove(this);
        Hide = false;
    }
}