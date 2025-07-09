// Ignore Spelling: cgm ig

using Eliminator;
using Eliminator.Network.ProcessedPackets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EliminatorApp;

public class Game1: Game
{
    private const string CARD_IMG_FOLDER = "CardImages/";
    private readonly GraphicsDeviceManager _graphics;
    private readonly IClientGameManager _serverComm;
    private readonly ProcessedInitialiseGamePacket _gameConfig;
    private SpriteBatch _spriteBatch;
    private GameStateMachine _gameStateMachine;
    private DeckView _deckView;

    /// <summary>
    /// TODO: Remove when no longer needed
    /// </summary>
    #region DebuggingParams
    private readonly TimeSpan _span = new(0, 0, 0, 0, 250);
    private readonly int _lowestFrameCount = 9999;
    private readonly int _removals = 0;
    private int _frameCount = 0;
    private SpriteFont _arial;
    private string _debugString = "None assigned";
    private readonly TimeSpan _prevSwitchTime = TimeSpan.Zero;
    private Color _intersectColor;
    #endregion

    private bool _inputLockRight = false;
    private bool _inputLockLeft = false;

    private float _screenScale = 1.0f;
    private Matrix _spriteScaleMatrix = Matrix.Identity;

    private readonly List<IView> _views = [];
    private readonly List<IButton> _miscButtons = [];
    private List<HandView> _handViews = [];
    private InputValidator _inputValidator;

    // Speed up / simplify calculations in HandView / FixedCard. Change these when the images change!
    // TODO: Alternatively, have HandView / FixedCard store this themselves from their texture that they get.
    public const int CARD_WIDTH = 64;
    public const int CARD_HEIGHT = 96;
    public const int CARD_HALF_DIAGONAL = 58;
    public static Vector2 CARD_CENTRE { get; } = new(CARD_WIDTH / 2, CARD_HEIGHT / 2);

    /// <summary>
    /// All loaded card textures
    /// </summary>
    public static Dictionary<CardValue, Texture2D> CardTextures { get; } = [];

    /// <summary>
    /// Border highlight textures for cards
    /// </summary>
    public static Dictionary<Color, Texture2D> HighlightTextures { get; } = [];

    /// <summary>
    /// If this is ever displayed, something has gone wrong, but it shouldn't be that dangerous
    /// </summary>
    public static Texture2D NoTexture { get; private set; }

    /// <summary>
    /// Contains all currently selected cards and hands
    /// Typically it is bad form to expose a <see cref="List{T}"/> as static but here I don't care
    /// Actually making this public at all may be a bad idea. Too bad!
    /// </summary>
    public static List<IButton> InputRegistry { get; } = [];

    public event EventHandler? InputRegistryChangedEvent;
    public event EventHandler? DeckClickedEvent;
    public event EventHandler<CardValue?> DiscardCardEvent;

    public Game1(IClientGameManager cgm, ProcessedInitialiseGamePacket igPacket)
    {
        _graphics = new GraphicsDeviceManager(this);
        _serverComm = cgm;
        _gameConfig = igPacket;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Eliminator";
        Window.ClientSizeChanged += UpdateScaleMatrix;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        Debug.Assert(_serverComm.HandManager != null,
            "ClientGameManager should be initialised prior to Game initialisation");
        _gameStateMachine = new(_serverComm.HandManager, _serverComm.PlayerId);
        _handViews = [.. MakeHandViews()];
        _arial = Content.Load<SpriteFont>("MyTextFont");

        _inputValidator = new InputValidator(_serverComm.HandManager);

        _serverComm.StartTurnEvent += (object? sender, ProcessedStartTurnPacket? packet) =>
        {
            if (packet != null)
            {
                _debugString = "ProcessedStartTurnPacket received, fired trigger on state machine";
                _gameStateMachine.FireStartTurnTrigger(packet.PlayerId);
            }
            else
            {
                Debug.WriteLine("Bad ProcessedStartTurnPacket received");
            }
        };

        _gameStateMachine.StateChanged += UpdateValidInputs;
        UpdateValidInputs(_gameStateMachine, _gameStateMachine.CurrentState);

        DeckClickedEvent += OnDeckClicked;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        var screenHeight = GraphicsDevice.DisplayMode.TitleSafeArea.Height;
        var screenWidth = GraphicsDevice.DisplayMode.TitleSafeArea.Width;
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _graphics.PreferredBackBufferHeight = screenHeight;
        _graphics.PreferredBackBufferWidth = screenWidth;
        _graphics.IsFullScreen = false;

        //_graphics.ToggleFullScreen(); Doesn't work how I want
        _graphics.ApplyChanges();

        IEnumerable<CardValue> enums = Enum.GetValues<CardValue>().Cast<CardValue>();
        for (var i = 0; i < enums.Count(); i++)
        {
            CardValue currentEnum = enums.ElementAt(i);
            CardTextures.Add(currentEnum, Content.Load<Texture2D>(CARD_IMG_FOLDER + currentEnum.ToString()));
        }

        NoTexture = CreateTexture(GraphicsDevice, 64, 96, Color.HotPink);

        HighlightTextures[Color.Gray] = CreateBorderedTexture(_graphics.GraphicsDevice, 64, 96, 5, Color.Gray, Color.Transparent);
        HighlightTextures[Color.Orange] = CreateBorderedTexture(_graphics.GraphicsDevice, 64, 96, 5, Color.Orange, Color.Transparent);
        HighlightTextures[Color.Green] = CreateBorderedTexture(_graphics.GraphicsDevice, 64, 96, 5, Color.Green, Color.Transparent);

        var deckDisplayHeight = CARD_HEIGHT + 20;
        _deckView = new DeckView(
                 _serverComm.HandManager.Deck,
                 _arial,
                 DeckClickedEvent,
                 new ButtonId(),
                 new RenderTarget2D(_graphics.GraphicsDevice, CARD_WIDTH, deckDisplayHeight),
                 new DisplaySpace(new((screenWidth / 2) - CARD_WIDTH, (screenHeight / 2) - deckDisplayHeight), 0f));

        _miscButtons.Add(_deckView);
        _views.Add(_deckView);
        _views.AddRange(_handViews);
    }

    // NOTE: Monogame automatically tries to run at 60hz
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        KeyboardState keyBoardState = Keyboard.GetState();
        MouseState cursor = Mouse.GetState();

        // TODO: Remove this debugging code
        //HandView investigate = _handViews.ToList()[2];
        //_perceivedFinalMousePosition = new(cursor.X / _screenScale, cursor.Y / _screenScale);
        //_perceivedFinalMousePosition -= investigate.DisplaySpace.Position;
        //_perceivedFinalMousePosition = Vector2.RotateAround(
        //    _perceivedFinalMousePosition,
        //    new(investigate.View.Width / 2, investigate.View.Height / 2),
        //    -investigate.DisplaySpace.Rotation);

        if (cursor.LeftButton == ButtonState.Pressed && !_inputLockLeft)
        {
            AttemptClick(cursor);
        }

        if (cursor.RightButton == ButtonState.Pressed && !_inputLockRight)
        {
            TryCancel();
            _inputLockRight = true;
        }

        if (cursor.RightButton == ButtonState.Released)
        {
            _inputLockRight = false;
        }

        if (cursor.LeftButton == ButtonState.Released)
        {
            _inputLockLeft = false;
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Method to check intersections and make any click(s) required
    /// </summary>
    /// <param name="cursor"> The cursor state at the time of clicking </param>
    private void AttemptClick(MouseState cursor)
    {
        Vector2 transformedPoint = new(cursor.X / _screenScale, cursor.Y / _screenScale);
        _handViews.ForEach(hand =>
        {
            if (hand.CheckIntersection(transformedPoint))
            {
                if (_gameStateMachine.CurrentState == GameState.Scramble)
                {
                    // All hands always clickable in Scramble state
                    hand.Click();
                    return;
                }

                // TODO: Maybe only need to check clickable cards, depending on how HandleCardClicked shakes out
                var cardIntersections = hand.GetCardIntersections(transformedPoint, false).ToList();
                if (cardIntersections.Count > 0) // TODO: This should always be 1 or 0. Log warning if above 1?
                {
                    foreach (FixedCard? button in cardIntersections.Where(card => card.Clickable))
                    {
                        button.Click();
                    }

                    HandleCardClicked(true);
                }

                _inputLockLeft = true;
                HandleCardClicked(false); // Can't remember why I called this when false...
                return;
            }
        });

        _miscButtons
            .Where(button => button.Clickable)
            .FirstOrDefault(button => button.CheckIntersection(transformedPoint))?
            .Click();
    }

    /// <summary>
    /// Informs <see cref="GameStateMachine"/> of selection changes, and/or makes requests to send packets
    /// via the <see cref="IClientGameManager"/>. NOTE: Not all objects that look like a card on screen are <see cref="Card"/>s
    /// This function does not handle all <see cref="IButton"/> presses, only explicit <see cref="FixedCard"/> presses
    /// </summary>
    /// <param name="clickHappened"></param>
    private void HandleCardClicked(bool clickHappened)
    {
        if (_gameStateMachine.CurrentState is not GameState.DeckDraw)
        {
            if (clickHappened && InputRegistry.Count > 0)
            {
                _gameStateMachine.FireSelectionUpdateTrigger(InputRegistry);
                return;
            }
        }

        if (InputRegistry.Count != 1)
        {
            Debug.WriteLine($"Card clicked while in {_gameStateMachine.CurrentState} state, but InputRegistry empty!");
            return;
        }

        // TODO: I think the below logic needs moving or changing. THis function is broken
        // because it allows Swap even when Card not clickable i.e. clickHappened False

        //if (_serverComm.HandManager.TopDiscardCardId == InputRegistry.First().ButtonId.Value)
        //{
        //    _serverComm.SendDiscardPacket();
        //    //TODO: Add event for managing a discard anim occurring
        //    CardValue? heldValue = Card.GetNumber(_serverComm.HandManager.HeldCardId);
        //    Card.ChangePlaceholderNumber(_serverComm.HandManager.TopDiscardCardId, heldValue);
        //    Card.ChangePlaceholderNumber(_serverComm.HandManager.HeldCardId, null);
        //    DiscardCardEvent?.Invoke(this, heldValue);
        //    return;
        //}

        //_serverComm.SendSwapPacket((ushort)InputRegistry.First().ButtonId.Value, _serverComm.HandManager.TopDiscardCardId);
    }

    // performance suffers when creating 50+ NEW textures per frame
    // so generating non-saved textures for hovering is fine generally
    protected override void Draw(GameTime gameTime)
    {
        _frameCount++;

        _views.ForEach(view => view.Draw(GraphicsDevice, _spriteBatch));

        GraphicsDevice.SetRenderTarget(null); // back to default render target
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Using defaults except scaling
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            null,
            null,
            null,
            null,
            null,
            _spriteScaleMatrix);

        _views.ForEach(view =>
        {
            var origin = new Vector2(view.View.Width / 2, view.View.Height / 2);
            _spriteBatch.Draw(
                view.View,
                view.DisplaySpace.Position + origin,
                null,
                Color.White,
                view.DisplaySpace.Rotation,
                origin,
                1.0f,
                SpriteEffects.None,
                0f);
        });

        var output = _gameStateMachine.CurrentState.ToString();
        _spriteBatch.DrawString(
            _arial,
            output,
            new(_graphics.PreferredBackBufferWidth - _arial.MeasureString(output).X - 10, _arial.MeasureString(output).Y * 2),
            Color.Green);

        _spriteBatch.DrawString(
            _arial,
            _debugString,
            new(_graphics.PreferredBackBufferWidth - _arial.MeasureString(_debugString).X - 10, _arial.MeasureString(_debugString).Y * 4),
            Color.Black);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // TODO: the skip, pass and call buttons
    private void UpdateValidInputs(object? sender, GameState state)
    {
        // Exclude already selected cards, all cancellation done through right click/separate action
        IEnumerable<int> validIds =
            _inputValidator
            .GetValidCardIds(state, _serverComm.PlayerId, _serverComm.TurnPlayerId)
            .Select(id => (int)id)
            .Except(InputRegistry.Select(button => button.ButtonId.Value));

        _debugString = "Ids: ";
        foreach (var item in validIds)
        {
            _debugString += item.ToString();
        }

        var allHandsCards = _handViews.SelectMany(hand => hand.DisplayCards).ToList();
        allHandsCards.ForEach(card =>
        {
            card.Clickable = validIds.Contains(card.RepresentedCard.Id);
        });

        IEnumerable<IButton> nonCardButtons = [.. _miscButtons];
        nonCardButtons = nonCardButtons.Concat(_handViews);
        nonCardButtons.ToList().ForEach(button => button.Clickable = false);

        switch (_gameStateMachine.CurrentState)
        {
            case GameState.Scramble:
                _handViews.ForEach(hand => hand.Clickable = true);
                break;
            case GameState.QuickPlace:
                _deckView.Clickable = true;
                break;
            default:
                break;
        }
    }

    private void OnDeckClicked(object? sender, EventArgs e)
    {
        _gameStateMachine.FireDeckClickTrigger();
        if (_gameStateMachine.CurrentState == GameState.TurnStart)
        {
            _serverComm.SendDrawPacket();
            return;
        }

        if (_gameStateMachine.CurrentState == GameState.QuickPlace)
        {
            _serverComm.SendQuickPlacePacket((ushort)InputRegistry.First().ButtonId.Value);
        }
    }

    // TODO: Clean up based on what cancelling actually requires e.g. are we calling this twice when receiving packet or nah
    // And should we auto cancel on 0 inputs for anything?
    // At the moment, if something is cancelled you don't get the option to reselect. This is no good, as we need this
    private void TryCancel()
    {
        if (InputRegistry.Count > 0)
        {
            IButton[] buttons = [.. InputRegistry];
            // Buttons in the input registry are put there with a click and removed with a click
            foreach (IButton button in buttons)
            {
                button.Click();
            }

            return;
        }

        if (_gameStateMachine.CanFireCancelTrigger())
        {
            _gameStateMachine.FireCancelActionTrigger();
        }
    }

    private void UpdateScaleMatrix(object? sender, EventArgs e)
    {
        // Default resolution is 1920x1080; scale sprites up or down based on
        // current viewport.
        // TODO: Add black bars for screens not adhering to 16:9 ratio (which is required)
        _screenScale = _graphics.GraphicsDevice.Viewport.Width / 1920f;
        // Create the scale transform for Draw.
        // Do not scale the sprite depth (Z=1).
        // Do not the cat.
        _spriteScaleMatrix = Matrix.CreateScale(_screenScale, _screenScale, 1);
    }

    private static Texture2D CreateBorderedTexture(GraphicsDevice device, int width, int height, int borderWidth, Color outerColor, Color innerColor)
    {
        // Initialize a texture
        var texture = new Texture2D(device, width, height);

        // The array holds the color for each pixel in the texture
        var data = new Color[width * height];
        for (var pixel = 0; pixel < data.Length; pixel++)
        {
            var column = pixel % width;
            var row = pixel / width;

            // Test if pixel lies on the border - both 
            data[pixel] = column < borderWidth || // left edge
                column >= width - borderWidth || // right edge
                row < borderWidth || // top edge
                row >= height - borderWidth // bottom edge
                ? outerColor
                : innerColor;
            //data[pixel] = pixel % 5.0 == 0 ? Color.White : Color.Transparent;
        }

        // Draw a red spike on the left
        for (var pixel = 0; pixel < data.Length; pixel++)
        {
            var column = pixel % width;
            var row = pixel / width;

            if (!(column == 0 && row == (height / 2) + 9)) // lower middle left
            {
                continue;
            }

            data[pixel] = Color.Red;

            for (var i = 1; i < borderWidth; i++) // horizontally expand backwards into the shape, which from the left means addition
            {
                var spikeRoot = pixel + i;
                data[spikeRoot] = Color.Red;
                for (var j = 1; j < i; j++) // expanding outwards from the root (giving it width)
                {
                    var laterPixel = spikeRoot + (j * width);
                    var earlierPixel = spikeRoot - (j * width);
                    if (laterPixel < data.Length) // prevent trying to draw the spike outside the texture bounds
                    {
                        data[spikeRoot + (j * width)] = Color.Red;
                    }

                    if (earlierPixel < data.Length && earlierPixel > 0)
                    {
                        data[spikeRoot - (j * width)] = Color.Red;
                    }
                }
            }
        }

        var brPixel = data.Length; // tip of triangle on the bottom right of the texture
        var blPixel = brPixel - (borderWidth * 2); // left extremity of triangle

        for (var i = 0; blPixel < brPixel; blPixel++) // coloring bottom row of triangle and upwards
        {
            data[blPixel] = Color.Blue;
            for (var j = 0; j < i; j++)
            {
                data[blPixel - (j * width)] = Color.Blue;
            }

            i++;
        }

        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// Make a collection of <see cref="HandView"/>s sized and positioned ideally for a 1920x1080 area (UpdateScaleArea accounts for differences)
    /// </summary>
    /// <param name="playerCount"> Number of <see cref="HandView"/>s to create </param>
    /// <returns> A container of <see cref="HandView"/>s </returns>
    private List<HandView> MakeHandViews()
    {
        Debug.Assert(_serverComm.HandManager != null,
            "ClientGameManager should be initialised prior to Game initialisation");
        var playerCount = _serverComm.HandManager.PlayerIds().Count;
        Debug.Assert(2 <= playerCount &&
                     playerCount <= 8,
                     "An Eliminator game is only set up for between 2 and 8 players");

        var screenWidth = 1920;
        var halfscrWidth = screenWidth / 2;
        var screenHeight = 1080;
        var halfscrHeight = screenHeight / 2;
        var viewWidth = 300;
        var halfViewWidth = viewWidth / 2;
        var viewHeight = 300;
        var halfViewHeight = viewHeight / 2;
        //var viewDiagonalLength = 212; TODO: YAGNI

        // The user's hand should always occupy the closest space (bottom centre)
        List<DisplaySpace> spaces = [new(new(halfscrWidth - halfViewWidth, screenHeight - viewHeight), 0)];

        // NOTE: Monogame appears to use clockwise rotation for some reason
        switch (playerCount)
        {
            case 2:
                spaces.Add(new(new(halfscrWidth - halfViewWidth, 0), (float)Math.PI));
                break;
            case 3:
                // TODO: Fix this ridiculousness
                spaces.Add(new(new(106, 184), -(float)Math.PI / 4f));
                spaces.Add(new(new(halfscrWidth + 106, 106), (float)Math.PI / 4f));
                break;
            case 4:
                // TODO: Fix LR looking squashed
                spaces.Add(new(new(halfscrWidth - halfViewWidth, 0), (float)Math.PI));
                spaces.Add(new(new(0, halfscrHeight - halfViewHeight), -(float)Math.PI / 2));
                spaces.Add(new(new(screenWidth - viewWidth, halfscrHeight - halfViewHeight), (float)Math.PI / 2));
                break;
            default:
                throw new NotImplementedException("Requested too many hand spaces, or too few");
        }

        // HandViews are distributed such that clockwise rotation is ascending ID (looping), but the bottom position is always the user
        var views = new List<HandView>();
        for (byte i = 0; i < playerCount; i++)
        {
            var assignPlayerId = (byte)(_serverComm.PlayerId + i);
            if (assignPlayerId >= playerCount)
            {
                assignPlayerId -= (byte)(_serverComm.PlayerId + 1);
            }

            views.Add(new(_serverComm.HandManager.GetCardsInHand(assignPlayerId),
                          new(_graphics.GraphicsDevice, 300, 300),
                          spaces[i],
                          assignPlayerId));
        }

        return views;
    }

    private static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Color colour)
    {
        // Initialize a texture
        var texture = new Texture2D(device, width, height);

        // The array holds the color for each pixel in the texture
        var data = new Color[width * height];

        for (var pixel = 0; pixel < data.Length; pixel++)
        {
            data[pixel] = colour;
        }

        texture.SetData(data);
        return texture;
    }
}
