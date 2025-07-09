using Eliminator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EliminatorApp;
public class DeckView: IButton, IView
{
    private readonly IDeck _deck;
    private readonly int _deckInitialSize;
    private readonly SpriteFont _font;
    private readonly Texture2D _cardBack;
    private readonly EventHandler _deckClickedEvent;

    public ButtonId ButtonId { get; }

    public bool Clickable { get; set; } = false;

    public RenderTarget2D View { get; }

    public DisplaySpace DisplaySpace { get; }

    public DeckView(IDeck deck,
                    SpriteFont font,
                    EventHandler deckClickedEvent,
                    ButtonId deckButtonID,
                    RenderTarget2D view,
                    DisplaySpace space)
    {
        _deck = deck;
        _deckInitialSize = _deck.StandardSizeMultiple * 54;
        _font = font;
        _cardBack = Game1.CardTextures[CardValue.Back];
        View = view;
        DisplaySpace = space;
        ButtonId = deckButtonID;
        _deckClickedEvent = deckClickedEvent;
    }

    public bool CheckIntersection(Vector2 point, float scale = 1)
    {
        Rectangle rect = new((int)DisplaySpace.Position.X,
                             (int)DisplaySpace.Position.Y,
                             (int)(_cardBack.Width * scale) + 1,
                             (int)(_cardBack.Height * scale) + 1);

        return rect.Contains(point);
    }

    public void Click() => _deckClickedEvent.Invoke(this, new());

    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        graphicsDevice.SetRenderTarget(View);
        graphicsDevice.Clear(Color.Gray);

        // TODO: Check this if performance issue
        var greenMult = _deck.Remaining / (float)_deckInitialSize;
        var redMult = 1.0f - greenMult;

        spriteBatch.Begin();

        if (_deck.Remaining > 0)
        {
            spriteBatch.Draw(
                _cardBack,
                new(),
                null,
                Color.White,
                0f,
                new(),
                1.0f,
                SpriteEffects.None,
                0f);
        }

        // TODO: This colour variation sucks and is stupid, fix it
        spriteBatch.DrawString(
            _font,
            $"{_deck.Remaining} / {_deckInitialSize}",
            new(0, _cardBack.Height),
            new(255f * redMult, 255f * greenMult, 0f),
            0f,
            new(),
            0.5f,
            SpriteEffects.None,
            0f);

        spriteBatch.End();
    }
}
