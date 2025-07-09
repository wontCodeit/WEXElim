using Eliminator.Network.ProcessedPackets;

namespace Eliminator;

public interface IClientGameManager: IDisposable
{
    public HandManager? HandManager { get; }
    public string Name { get; }
    public byte PlayerId { get; }
    public byte TurnPlayerId { get; }

    public event EventHandler<ProcessedDrawResultPacket?>? DrawResultEvent;
    public event EventHandler<ProcessedStartTurnPacket?>? StartTurnEvent;
    public event EventHandler<ProcessedConnectionResponsePacket?>? ConnectResponseEvent;
    public event EventHandler<ProcessedInitialiseGamePacket?>? InitialiseGameEvent;
    public event EventHandler<EventArgs>? FatalErrorEvent; // When there is something wrong that nothing can be done about :C
    public event EventHandler<ProcessedAssignIdPacket?>? AssignIdResponseEvent;

    public void SendConnectPacket(string userName);
    public void SendDrawPacket();
    public void SendDiscardPacket();
    public void SendSwapPacket(ushort cardId1, ushort cardId2);
    public void SendQuickPlacePacket(ushort cardId);

    public void BeginRun();
}