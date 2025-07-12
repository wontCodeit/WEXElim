using Eliminator;
using Eliminator.Network.ProcessedPackets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EliminatorApp;
internal class MockClientGameManager: IClientGameManager
{
    private const byte SERVER_ID = 255;
    private readonly HandManager _serverHM;

    public MockClientGameManager(string userName, byte id, ProcessedInitialiseGamePacket igPacket)
    {
        Name = userName;
        PlayerId = id;
        HandManager = new((byte)igPacket.Players.Count, igPacket.StartingCards, new BlankDeck(1), new CardCounter());
        _serverHM = new((byte)igPacket.Players.Count, igPacket.StartingCards, new Deck(1), new CardCounter());
    }

    public HandManager? HandManager { get; private set; }

    public string Name { get; init; }

    public byte PlayerId { get; }

    public byte TurnPlayerId { get; private set; } = 255;

    public event EventHandler<EventArgs>? FatalErrorEvent;
    public event EventHandler<ProcessedAssignIdPacket?>? AssignIdResponseEvent;
    public event EventHandler<ProcessedStartTurnPacket?>? StartTurnEvent;
    public event EventHandler<ProcessedConnectionResponsePacket?>? ConnectResponseEvent;
    public event EventHandler<ProcessedInitialiseGamePacket?>? InitialiseGameEvent;
    public event EventHandler<ProcessedDrawResultPacket?>? DrawResultEvent;

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

    public void SendConnectPacket(string userName)
    {
        PacketReader.ReadInternalPacket(new ProcessedConnectionResponsePacket(SERVER_ID, true, null));
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
                default:
                    throw new NotImplementedException("Mock received unexpected packet");

            }
        }
    }

    public void SendDrawPacket() => throw new NotImplementedException();
    public void SendDiscardPacket() => throw new NotImplementedException();
    public void SendSwapPacket(ushort cardId1, ushort cardId2) => throw new NotImplementedException();
    public void SendQuickPlacePacket(ushort cardId)
    {
        if (_serverHM.QuickPlace(cardId))
        {

        }
    }
}
