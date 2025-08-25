using Eliminator.Network.ProcessedPackets;

namespace Eliminator;

/// <summary>
/// Provides functionality for managing the Eliminator game excluding specifics like the GUI
/// Handles server communication, holds the <see cref="HandManager"/> and provides a host of events
/// which are the server-event driven part of this event-driven program
/// </summary>
public interface IClientGameManager: IDisposable
{
    public HandManager? HandManager { get; }
    public string Name { get; }
    public byte PlayerId { get; }
    public byte TurnPlayerId { get; }

    public CardValue? HeldCardValue { get; }

    // Realistically, during game play (i.e. outside of initialisation) this will never be null, so it shouldn't have been a place holder
    // TODO: Remove "placeholder" altogether (somehow)
    public CardValue TopDiscardValue { get; }

    // TODO: Not so sure all of these are nullable
    public event EventHandler<ProcessedGameEndPacket?>? GameEndEvent;
    public event EventHandler<ProcessedDisplayScramblePacket?>? DisplayScrambleEvent;
    public event EventHandler<ProcessedDisplayPeekPacket?>? DisplayPeekEvent;
    public event EventHandler<ProcessedPeekResultPacket?>? PeekResultEvent;
    public event EventHandler<ProcessedQuickPlaceResultPacket?>? QuickPlaceResultEvent;
    public event EventHandler<ProcessedDisplaySwapPacket?>? DisplaySwapEvent;
    public event EventHandler<ProcessedDiscardResultPacket?>? DiscardResultEvent;
    public event EventHandler<ProcessedDrawResultPacket?>? DrawResultEvent;
    public event EventHandler<ProcessedStartTurnPacket?>? StartTurnEvent;
    public event EventHandler<ProcessedConnectionResponsePacket?>? ConnectResponseEvent;
    public event EventHandler<ProcessedInitialiseGamePacket?>? InitialiseGameEvent;
    public event EventHandler<EventArgs>? FatalErrorEvent; // When there is something wrong that nothing can be done about :C
    public event EventHandler<ProcessedAssignIdPacket?>? AssignIdResponseEvent;

    public void SendConnectPacket(string userName);
    public void SendDisconnectPacket();
    public void SendDrawPacket();
    public void SendQuickPlacePacket(ushort cardId);
    public void SendDiscardPacket();
    public void SendSwapPacket(ushort cardId1, ushort cardId2);
    public void SendPeekPacket(ushort cardId);
    public void SendScramblePacket(byte playerId);
    public void SendCallItPacket();

    public void DoDiscardSwap(ushort cardId);

    public void BeginRun();
}