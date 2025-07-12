using Eliminator.Network.ProcessedPackets;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Eliminator;

/// <summary>
/// Provides functionality for managing the Eliminator game excluding specifics like the GUI
/// Handles server communication, holds the <see cref="HandManager"/> and provides a host of events
/// which are the server-event driven part of this event-driven program
/// </summary>
/// TODO: Add additional methods for sending packets
public class ClientGameManager: IClientGameManager
{
    private readonly ServerAccess _serverAccess;
    private readonly CardCounter _counter = new();
    private bool disposedValue;

    /// <summary>
    /// Created with a <see cref="BlankDeck"/> because all <see cref="CardValue"/>s come from server
    /// </summary>
    public HandManager? HandManager { get; private set; } // Create with blank deck to avoid complication

    public string Name => _serverAccess.Username;
    public byte PlayerId => _serverAccess.Id;
    public byte TurnPlayerId { get; private set; } = 255;

    #region events
    public event EventHandler<ProcessedDrawResultPacket?>? DrawResultEvent;
    public event EventHandler<ProcessedStartTurnPacket?>? StartTurnEvent;
    public event EventHandler<ProcessedConnectionResponsePacket?>? ConnectResponseEvent;
    public event EventHandler<ProcessedInitialiseGamePacket?>? InitialiseGameEvent;
    public event EventHandler<EventArgs>? FatalErrorEvent; // When there is something wrong that nothing can be done about :C
    public event EventHandler<ProcessedAssignIdPacket?>? AssignIdResponseEvent;
    #endregion

    public ClientGameManager(string dnsHostName, int dnsPort)
    {
        IPAddress ip = Dns.GetHostEntry(dnsHostName).AddressList.Last();
        //var ip = IPAddress.Parse("127.0.0.1"); if created on server host machine use this
        var tcpClient = new TcpClient();
        tcpClient.Connect(ip, dnsPort);
        Console.WriteLine("Connected tcpClient to server");
        _serverAccess = new(tcpClient);

        AssignIdResponseEvent += OnAssignId;
        InitialiseGameEvent += OnInitialiseGame;
        StartTurnEvent += OnStartTurn;
        DrawResultEvent += OnDrawResult;
    }

    public void SendConnectPacket(string userName)
    {
        _serverAccess.SendPacket(PacketWriter.WriteConnectionPacket(userName));
        Debug.WriteLine($"Sent connect packet");
    }

    public void SendDrawPacket()
    {
        _serverAccess.SendPacket(PacketWriter.WriteDrawPacket());
        Debug.WriteLine($"Sent draw packet");
    }

    public void SendDiscardPacket()
    {
        _serverAccess.SendPacket(PacketWriter.WriteDiscardPacket());
        HandManager.DiscardHeldCard();
        Debug.WriteLine($"Sent discard packet");
    }

    public void SendSwapPacket(ushort cardId1, ushort cardId2) => throw new NotImplementedException();

    public void BeginRun()
    {
        _serverAccess.Start();
        _ = Task.Run(Run);
    }

    private void Run()
    {
        while (true)
        {
            IProcessedPacket newPacket = PacketReader.GetNextPacket();

            switch (newPacket.OpCode)
            {
                case OpCode.AssignId:
                    AssignIdResponseEvent?.Invoke(this, newPacket as ProcessedAssignIdPacket);
                    break;
                case OpCode.ConnectionResponse:
                    ConnectResponseEvent?.Invoke(this, newPacket as ProcessedConnectionResponsePacket);
                    break;
                case OpCode.InitialiseGame:
                    InitialiseGameEvent?.Invoke(this, newPacket as ProcessedInitialiseGamePacket);
                    break;
                case OpCode.StartTurn:
                    StartTurnEvent?.Invoke(this, newPacket as ProcessedStartTurnPacket);
                    // TODO: Find out what the heck will happen here
                    // Is MonoGame drawing on a GUI thread? How will this interact with sprite batches? Guess we find out!
                    // If it is bad, make a check for a packet then if there is one process just the one, 1/update loop
                    break;
                case OpCode.DrawResult:
                    DrawResultEvent?.Invoke(this, newPacket as ProcessedDrawResultPacket);
                    break;
                default:
                    Debug.WriteLine("Received packet with no known response");
                    // TODO: Add better logging and perhaps this is too dangerous to not fail on
                    break;
            }
        }
    }

    private void OnAssignId(object? sender, ProcessedAssignIdPacket? aidPacket)
    {
        if (aidPacket is null)
        {
            Debug.WriteLine("Malformed assign id packet");
            return;
        }

        _serverAccess.AssignId(aidPacket.AssignId);
    }

    private void OnInitialiseGame(object? sender, ProcessedInitialiseGamePacket? igPacket)
    {
        if (igPacket is null)
        {
            Debug.WriteLine("Malformed initialise game packet");
            return;
        }

        HandManager = new((byte)igPacket.Players.Count, igPacket.StartingCards, new BlankDeck(igPacket.DeckSize), _counter);
    }

    private void OnStartTurn(object? sender, ProcessedStartTurnPacket? stPacket)
    {
        if (stPacket == null)
        {
            Debug.WriteLine("Malformed start turn packet");
            return;
        }

        Console.WriteLine("Received start turn packet, id: " + stPacket.PlayerId);
        TurnPlayerId = stPacket.PlayerId;
    }

    private void OnDrawResult(object? sender, ProcessedDrawResultPacket? drPacket)
    {
        if (drPacket is null)
        {
            Debug.WriteLine("Draw result event fired, packet was null");
            return;
        }

        HandManager.DrawCard();
        _counter.ChangePlaceholderNumber(HandManager.HeldCardId, drPacket.CardValue);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            _serverAccess.Dispose();
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~ClientGameManager()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void SendQuickPlacePacket(ushort cardId) => throw new NotImplementedException();
}
