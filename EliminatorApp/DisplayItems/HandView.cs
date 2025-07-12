using Eliminator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace EliminatorApp;
public class HandView: ICardContainer, IButton
{
    private readonly Stack<(DisplaySpace space, int index)> _emptySpaces = [];
    private readonly List<FixedCard> _displayCards = [];

    private float _viewScale = 1.0f;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ButtonId ButtonId { get; }

    /// <summary>
    /// The id of the hand this <see cref="HandView"/> represents
    /// </summary>
    public byte HandID { get; }

    /// <summary>
    /// This <see cref="HandView"/>'s space
    /// </summary>
    public DisplaySpace DisplaySpace { get; }

    /// <summary>
    /// 
    /// </summary>
    public RenderTarget2D View { get; private set; }

    /// <summary>
    /// The <see cref="FixedCard"/>s that this <see cref="HandView"/> instance is responsible for drawing
    /// </summary>
    public IEnumerable<FixedCard> DisplayCards => _displayCards;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Clickable { get; set; } = false;

    /// <summary>
    /// Constructs a <see cref="HandView"/>
    /// </summary>
    /// <param name="cards"></param>
    /// <param name="view"></param>
    /// <param name="space"></param>
    /// <param name="handId"></param>
    public HandView(
        IEnumerable<ICard> cards,
        RenderTarget2D view,
        DisplaySpace space,
        byte handId)
    {
        View = view;
        DisplaySpace = space;
        HandID = handId;
        ButtonId = new();

        foreach (ICard card in cards)
        {
            _ = AddFixedCard(card);
        }
    }

    /// <summary>
    /// Remove a <see cref="FixedCard"/> from those represented by this instance, freeing up a <see cref="DisplaySpace"/>
    /// </summary>
    /// <param name="card"> The <see cref="Card"/> that the <see cref="FixedCard"/> that will be removed represents </param>
    /// <returns> Whether the <see cref="Card"/> was found and by extension whether the <see cref="FixedCard"/> was removed </returns>
    public bool RemoveFixedCard(ICard card)
    {
        if (!_displayCards.Select(dcard => dcard.RepresentedCard.Id).Contains(card.Id))
        {
            return false;
        }

        FixedCard? cardToRemove = null;
        var index = 0;

        for (var i = 0; i < _displayCards.Count; i++)
        {
            FixedCard consideredCard = _displayCards[i];
            if (consideredCard.RepresentedCard.Id != card.Id)
            {
                continue;
            }

            cardToRemove = consideredCard;
            index = i;
        }

        if (cardToRemove is null)
        {
            return false;
        }

        _ = _displayCards.Remove(cardToRemove);
        _emptySpaces.Push((cardToRemove.DisplaySpace, index));
        return true;
    }

    /// <summary>
    /// Adds a new <see cref="FixedCard"/> to the <see cref="DisplayCards"/> of this instance
    /// </summary>
    /// <param name="card"> The <see cref="Card"/> to make a <see cref="FixedCard"/> from </param>
    /// <returns> The <see cref="FixedCard"/> instance that was just added </returns>
    public FixedCard AddFixedCard(ICard card)
    {
        (DisplaySpace space, var index) = TakeNextInternalSpace();

        var fcardToAdd = new FixedCard(card, space);

        _displayCards.Insert(index, fcardToAdd);

        return fcardToAdd;
    }

    /// <summary>
    /// Find the next <see cref="DisplaySpace"/> that the next given <see cref="Card"/> will be sent to
    /// </summary>
    /// <returns> Null if no available space, otherwise the relative next space </returns>
    public DisplaySpace? NextInternalSpace()
    {
        var scaledWidth = Game1.CARD_WIDTH * _viewScale;
        var scaledHeight = Game1.CARD_HEIGHT * _viewScale;
        var scaledSpacing = 10 * _viewScale;

        var usingX = scaledSpacing;
        var usingY = scaledSpacing;

        // Fill any space which has been taken from (holes in the rows)
        if (_emptySpaces.Count != 0)
        {
            DisplaySpace freeSpace = _emptySpaces.Peek().space;
            usingX = freeSpace.Position.X;
            usingY = freeSpace.Position.Y;
        }
        // Determine next new position in the same row
        else if (_displayCards.Count != 0)
        {
            usingX = _displayCards.Last().DisplaySpace.Position.X + scaledWidth + scaledSpacing;
            usingY = _displayCards.Last().DisplaySpace.Position.Y;
        }

        // If the right edge of the next card would exceed the width of the view, put it on the next level down.
        var rect = new Rectangle((int)(usingX + scaledWidth), (int)usingY, 1, 1);
        if (!View.Bounds.Intersects(rect))
        {
            usingX = scaledSpacing;
            usingY = usingY + scaledHeight + scaledSpacing;
        }

        // If the bottom right edge of the decided on next space is not within the bounds of the View i.e. every row is filled, returns null
        var edgeOfNextSpace = new Point((int)(usingX + scaledWidth), (int)(usingY + scaledHeight));
        return View.Bounds.Intersects(new(edgeOfNextSpace, new(1)))
            ? new DisplaySpace(new(usingX, usingY), 0f)
            : null;
    }

    /// <summary>
    /// Draw all the cards in this <see cref="HandView"/>. Note that the <see cref="GraphicsDevice"/> will be set to the <see cref="View"/> of this instance
    /// Also note that FixedCard instances are never rotated and so aren't drawn with the correct origin for rotation
    /// </summary>
    /// <param name="spriteBatch"> The game's <see cref="SpriteBatch"/>. No batch should be in progress when passed </param>
    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        graphicsDevice.SetRenderTarget(View);
        graphicsDevice.Clear(Color.Transparent);

        spriteBatch.Begin();

        _displayCards.ToList().ForEach(card =>
        {
            Color drawColor = Color.White;

            if (card.Hide)
            {
                drawColor = Color.Green;
            }

            spriteBatch.Draw(
                card.Texture,
                card.DisplaySpace.Position,
                null,
                drawColor,
                0f,
                new(),
                _viewScale,
                SpriteEffects.None,
                0f);

            // TODO: Move this logic, or something like it, out into Game1 OR add property for whether buttons are hovered
            if (card.Clickable)
            {
                spriteBatch.Draw(
                    Game1.HighlightTextures[Color.Orange],
                    card.DisplaySpace.Position,
                    null,
                    drawColor,
                    0f,
                    new(),
                    _viewScale,
                    SpriteEffects.None,
                    0f);
            }
            else
            {
                spriteBatch.Draw(
                    Game1.HighlightTextures[Color.Gray],
                    card.DisplaySpace.Position,
                    null,
                    drawColor,
                    0f,
                    new(),
                    _viewScale,
                    SpriteEffects.None,
                    0f);
            }
        });

        spriteBatch.End();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool CheckIntersection(Vector2 point, float scale = 1.0f)
    {
        Rectangle rectangle = new((int)DisplaySpace.Position.X, (int)DisplaySpace.Position.Y, View.Width, View.Height);
        return rectangle.RotatedIntersects(DisplaySpace.Rotation, point);
    }

    /// <summary>
    /// Get all the cards which the given point intersects with.
    /// </summary>
    /// <param name="point"> The point to check against. Note that this should already be transformed by the screen scale </param>
    /// <returns> All <see cref="FixedCard"/>s which the point lies within the region of </returns>
    public IEnumerable<FixedCard> GetCardIntersections(Vector2 point, bool mustBeClickable)
    {
        // Set position to be relative to View 0,0 and account for 
        Vector2 transformedPoint = new Vector2(point.X, point.Y) - DisplaySpace.Position;

        // For some reason, Monogame draw will rotate clockwise when in radians you usually go anti-clockwise
        // so this vector method needs negative (clockwise) rotation
        var rotatedPoint = Vector2.RotateAround(transformedPoint, new(View.Width / 2, View.Height / 2), -DisplaySpace.Rotation);

        IEnumerable<FixedCard> cardsToCheck = mustBeClickable ? _displayCards.Where(card => card.Clickable) : _displayCards;
        return cardsToCheck.Where(card => card.CheckIntersection(rotatedPoint, _viewScale));
    }

    /// <summary>
    /// Adds this to the <see cref="Game1"/> input registry, or remove it if it is already there
    /// </summary>
    public void Click()
    {
        if (!Clickable)
        {
            return;
        }

        if (Game1.InputRegistry.Contains(this))
        {
            _ = Game1.InputRegistry.Remove(this);
            return;
        }

        Game1.InputRegistry.Add(this);
    }

    private (DisplaySpace space, int index) TakeNextInternalSpace()
    {
        if (_emptySpaces.TryPop(out (DisplaySpace space, int index) space))
        {
            return space;
        }

        while (NextInternalSpace() is null)
        {
            UpdateDisplaySpaces();
        }

        return ((DisplaySpace)NextInternalSpace(), _displayCards.Count);
    }

    // Shrink the view space then insert all the cards again so that more can be shown at once (albeit smaller)
    private void UpdateDisplaySpaces()
    {
        _viewScale *= 0.9f;
        var currentCards = _displayCards.ToList();
        _displayCards.Clear();
        currentCards.ForEach(fcardToAdd =>
        {
            FixedCard addedCard = AddFixedCard(fcardToAdd.RepresentedCard);
            addedCard.Clickable = fcardToAdd.Clickable;
            addedCard.Hide = fcardToAdd.Hide;
        });
    }
}