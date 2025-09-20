using Eliminator;
using Eliminator.Network.ProcessedPackets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EliminatorApp;

/// <summary>
/// <inheritdoc cref="IClientGameManager"/>
/// </summary>
internal class MockClientGameManager: IClientGameManager
{
    private const byte SERVER_ID = 255;
    private readonly HandManager _serverHM;
    private readonly CardCounter _serverCardCounter = new();
    private readonly CardCounter _clientCardCounter = new();

    public HandManager? HandManager { get; private set; }

    public string Name { get; init; }

    public byte PlayerId { get; }

    public byte TurnPlayerId { get; private set; } = 255;

    public CardValue? HeldCardValue => _clientCardCounter.GetNumber(HandManager.HeldCardId);

    public CardValue TopDiscardValue => _clientCardCounter.GetNumber(HandManager.TopDiscardCardId) ?? CardValue.Back; // after initialisation, is not null

    #region events
    public event EventHandler<EventArgs>? FatalErrorEvent;
    public event EventHandler<ProcessedAssignIdPacket?>? AssignIdResponseEvent;
    public event EventHandler<ProcessedStartTurnPacket?>? StartTurnEvent;
    public event EventHandler<ProcessedConnectionResponsePacket?>? ConnectResponseEvent;
    public event EventHandler<ProcessedInitialiseGamePacket?>? InitialiseGameEvent;
    public event EventHandler<ProcessedDrawResultPacket?>? DrawResultEvent;
    public event EventHandler<ProcessedGameEndPacket?>? GameEndEvent;
    public event EventHandler<ProcessedDisplayScramblePacket?>? DisplayScrambleEvent;
    public event EventHandler<ProcessedDisplayPeekPacket?>? DisplayPeekEvent;
    public event EventHandler<ProcessedPeekResultPacket?>? PeekResultEvent;
    public event EventHandler<ProcessedQuickPlaceResultPacket?>? QuickPlaceResultEvent;
    public event EventHandler<ProcessedDisplaySwapPacket?>? DisplaySwapEvent;
    public event EventHandler<ProcessedDiscardResultPacket?>? DiscardResultEvent;
    #endregion

    public MockClientGameManager(string userName, byte id, ProcessedInitialiseGamePacket igPacket)
    {
        Name = userName;
        PlayerId = id;
        HandManager = new((byte)igPacket.Players.Count, igPacket.StartingCards, new BlankDeck(1), _clientCardCounter);

        List<CardValue> firstCards = [
            CardValue.ClubsJack, // p1, card 1 (leftmost)
            CardValue.SpadesFive,
            CardValue.SpadesFour,
            CardValue.SpadesThree,

            CardValue.SpadesTwo, // p2
            CardValue.SpadesAce,
            CardValue.ClubsAce,
            CardValue.ClubsTwo,

            CardValue.ClubsThree, // p3
            CardValue.ClubsFour,
            CardValue.ClubsFive,
            CardValue.ClubsSix,

            CardValue.SpadesSix];

        _serverHM = new((byte)igPacket.Players.Count, igPacket.StartingCards, new RiggedDeck(1, firstCards), _serverCardCounter);

        // We would usually use the igPacket's discard card, but in this case it doesn't align with the server deck so it is ignored
        _serverHM.DrawCard();
        _serverHM.DiscardHeldCard();
        var initialDiscard = (CardValue)_serverCardCounter.GetNumber(_serverHM.TopDiscardCardId)!;
        HandManager.DrawCard();
        HandManager.DiscardHeldCard();
        _serverCardCounter.ChangePlaceholderNumber(HandManager.TopDiscardCardId, initialDiscard);
        _clientCardCounter.ChangePlaceholderNumber(HandManager.TopDiscardCardId, initialDiscard);

        DrawResultEvent += OnDrawResult;
        DiscardResultEvent += OnDiscardResult;
    }

    private void OnDiscardResult(object? sender, ProcessedDiscardResultPacket? discardResultPacket)
    {
        _clientCardCounter.ChangePlaceholderNumber(HandManager.TopDiscardCardId, discardResultPacket.CardValue);
        _clientCardCounter.ChangePlaceholderNumber(HandManager.HeldCardId, null);
    }

    private void OnDrawResult(object? sender, ProcessedDrawResultPacket? drPacket)
    {
        _clientCardCounter.ChangePlaceholderNumber(HandManager.HeldCardId, drPacket.CardValue);
    }

    public void BeginRun()
    {
        _ = Task.Run(Run);
    }

    public void MockOnly_ReceiveStartTurn(int afterMs = 1000)
    {
        Action addPacket = () =>
        {
            Thread.Sleep(afterMs);
            PacketReader.ReadInternalPacket(new ProcessedStartTurnPacket(255, PlayerId));
        };

        _ = Task.Run(addPacket);
    }

    public void Dispose()
    {
        // This mock doesn't need to do anything for disposal
    }

    // Simulates delay from server, but assumes always success(?)/simplest case
    private void Run()
    {
        while (true)
        {
            if (!PacketReader.NextPacketReady())
            {
                Thread.Sleep(100);
                continue;
            }

            IProcessedPacket newPacket = PacketReader.GetNextPacket();
            switch (newPacket.OpCode)
            {
                // Simulate opponent(s) all immediately ending their turn(s)
                case OpCode.PassTurn:
                    PacketReader.ReadInternalPacket(new ProcessedStartTurnPacket(SERVER_ID, PlayerId));
                    break;
                case OpCode.StartTurn:
                    StartTurnEvent?.Invoke(this, newPacket as ProcessedStartTurnPacket);
                    break;
                case OpCode.DrawResult:
                    DrawResultEvent?.Invoke(this, newPacket as ProcessedDrawResultPacket);
                    break;
                case OpCode.DiscardResult:
                    DiscardResultEvent?.Invoke(this, newPacket as ProcessedDiscardResultPacket);
                    break;
                case OpCode.QuickPlaceResult:
                    QuickPlaceResultEvent?.Invoke(this, newPacket as ProcessedQuickPlaceResultPacket);
                    break;
                case OpCode.PeekResult:
                    PeekResultEvent?.Invoke(this, newPacket as ProcessedPeekResultPacket);
                    break;
                case OpCode.DisplayScramble:
                    DisplayScrambleEvent?.Invoke(this, newPacket as ProcessedDisplayScramblePacket);
                    break;
                case OpCode.DisplaySwap:
                    DisplaySwapEvent?.Invoke(this, newPacket as ProcessedDisplaySwapPacket);
                    break;
                case OpCode.GameEnd:
                    GameEndEvent?.Invoke(this, newPacket as ProcessedGameEndPacket);
                    break;
                default:
                    throw new NotImplementedException("Mock received unexpected packet");

            }
        }
    }

    #region SendPacket functions
    public void SendConnectPacket(string userName)
    {
        PacketReader.ReadInternalPacket(new ProcessedConnectionResponsePacket(SERVER_ID, true, null));
    }

    public void SendDisconnectPacket() => throw new NotImplementedException();

    public void SendDrawPacket()
    {
        _serverHM.DrawCard();
        HandManager.DrawCard();

        // OnDrawResult: Assign to the Held card (until some other action is done)
        PacketReader.ReadInternalPacket(new ProcessedDrawResultPacket(SERVER_ID, (CardValue)_serverCardCounter.GetNumber(_serverHM.HeldCardId)));
    }

    public void SendDiscardPacket()
    {
        var discardedVal = (CardValue)_serverCardCounter.GetNumber(_serverHM.HeldCardId);
        _serverHM.DiscardHeldCard();
        HandManager.DiscardHeldCard();

        // OnDiscardResult: Assign to the Held card, send DoCardAction trigger to state machine (possibly through event, or some other way via Game1).
        PacketReader.ReadInternalPacket(new ProcessedDiscardResultPacket(SERVER_ID, discardedVal));
    }

    public void SendSwapPacket(ushort cardId1, ushort cardId2)
    {
        _serverHM.Swap(cardId1, cardId2);
        HandManager.Swap(cardId1, cardId2);
        PacketReader.ReadInternalPacket(new ProcessedDisplaySwapPacket(SERVER_ID, cardId1, cardId2));
        // OnDisplaySwap: No assignment, all GUI work only
        // Theoretically, this should not be necessary, as client knows to do this themself.
        // However, since responding to this event means handling of both opp and user doing a swap
        // It would make sense to simply send this and respond to it rather than add special handling
    }

    public void SendQuickPlacePacket(ushort cardId)
    {
        var cardValue = (CardValue)_serverCardCounter.GetNumber(cardId);
        var QPSuccess = _serverHM.QuickPlace(cardId);
        QuickPlaceResult QPResultAsEnum = QPSuccess ? QuickPlaceResult.Success : QuickPlaceResult.Failure;
        _clientCardCounter.ChangeNumber(cardId, cardValue);
        _ = HandManager.QuickPlace(cardId); // Handles removal (or not) from hand, but not Punishment for failure

        if (!QPSuccess)
        {
            _ = _serverHM.PunishPlayer(PlayerId);
            _ = HandManager.PunishPlayer(PlayerId);
        }

        // OnQuickPlaceResult:
        // With this, Client should see card with CardValue given fly from hand onto discard pile
        // and then the result can be used to show any punishment (gaining a card) that occurs
        PacketReader.ReadInternalPacket(new ProcessedQuickPlaceResultPacket(SERVER_ID, QPResultAsEnum, PlayerId, cardValue, cardId));

        if (QPResultAsEnum == QuickPlaceResult.Success)
        {
            // OnDiscardResult: Assign to the Held card, send DoCardAction trigger to state machine (possibly through event, or some other way via Game1).
            PacketReader.ReadInternalPacket(new ProcessedDiscardResultPacket(SERVER_ID, cardValue));
        }
    }

    public void SendPeekPacket(ushort cardId)
    {
        // OnPeekResult: Assign to the given card (for a finite duration!) and maybe send event for some animation to play
        PacketReader.ReadInternalPacket(new ProcessedPeekResultPacket(SERVER_ID, (CardValue)_serverCardCounter.GetNumber(cardId), cardId));
    }

    public void SendScramblePacket(byte playerId)
    {
        // OnScramble: Scramble in GUI
        _serverHM.Scramble(playerId);
        PacketReader.ReadInternalPacket(new ProcessedDisplayScramblePacket(SERVER_ID, playerId)); // Also necessary until server change
    }

    public void SendCallItPacket()
    {
        // OnGameEnd: Some sort of display signalled to Game1, this object to be disposed of, or if replayability CardCounter and HandManager to be replaced
        PacketReader.ReadInternalPacket(new ProcessedGameEndPacket(SERVER_ID, _serverHM.CalculateHandValues()));
    }

    public void DoDiscardSwap(ushort cardId)
    {
        var cv = (CardValue)_serverCardCounter.GetNumber(cardId)!;
        SendSwapPacket(cardId, _serverHM.TopDiscardCardId);
        PacketReader.ReadInternalPacket(new ProcessedDiscardResultPacket(SERVER_ID, cv));
    }

    public void SendPassItPacket()
    {
        PacketReader.ReadInternalPacket(new ProcessedPassTurnPacket(PlayerId));
    }
    #endregion
}
