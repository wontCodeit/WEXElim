using Eliminator.Network.ProcessedPackets;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace Eliminator;

/// <summary>
/// Class for reading raw (<see cref="byte"/>) packets and retrieval of processed packets (<see cref="IProcessedPacket"/>)
/// </summary>
public class PacketReader: BinaryReader
{
    // All instances read packets and store them in this same queue
    private static readonly ConcurrentQueue<IProcessedPacket> _waitingPackets = new();
    private readonly byte _senderId;

    /// <summary>
    /// Constructor for a <see cref="PacketReader"/>
    /// </summary>
    /// <param name="networkStream"> The given <see cref="NetworkStream"/> to be read from </param>
    public PacketReader(NetworkStream networkStream, byte senderId) : base(networkStream)
    {
        _senderId = senderId;
    }

    /// <summary>
    /// Retrieve the next <see cref="IProcessedPacket"/>
    /// </summary>
    /// <returns></returns>
    public static IProcessedPacket GetNextPacket()
    {
        var gotPacket = false;
        IProcessedPacket? packet = null;
        while (!gotPacket)
        {
            gotPacket = _waitingPackets.TryDequeue(out packet);
        }

        return packet!;
    }

    public static bool NextPacketReady()
    {
        return !_waitingPackets.IsEmpty;
    }

    /// <summary>
    /// Necessary for when a client loses connection unexpectedly or some other event generates a packet without a stream
    /// </summary>
    /// <param name="packet"> The given packet to enqueue </param>
    public static void ReadInternalPacket(IProcessedPacket packet)
    {
        _waitingPackets.Enqueue(packet);
    }

    /// <summary>
    /// Read from this <see cref="PacketReader"/>s given <see cref="NetworkStream"/> to store an <see cref="IProcessedPacket"/> for retrieval
    /// </summary>
    /// <param name="opCode"> The <see cref="OpCode"/> of the next packet in stream. Note that this must have been read and removed from the stream prior to this method call </param>
    /// <exception cref="InvalidOperationException"> Thrown when an unexpected <see cref="OpCode"/> is passed </exception>
    /// <exception cref="EndOfStreamException"> </exception>
    /// <exception cref="IOException"> </exception>
    /// <exception cref="ObjectDisposedException"> </exception>
    public void Read(OpCode opCode)
    {
        switch (opCode)
        {
            case OpCode.AssignId:
                ReadAssignId();
                break;
            case OpCode.Connect:
                ReadConnect();
                break;
            case OpCode.ConnectionResponse:
                ReadConnectionResponse();
                break;
            case OpCode.InitialiseGame:
                ReadInitialiseGame();
                break;
            case OpCode.Disconnection:
                ReadDisconnection();
                break;
            case OpCode.QuickPlace:
                ReadQuickPlace();
                break;
            case OpCode.QuickPlaceResult:
                ReadQuickPlaceResult();
                break;
            case OpCode.StartTurn:
                ReadStartTurn();
                break;
            case OpCode.Draw:
                ReadDraw();
                break;
            case OpCode.DrawResult:
                ReadDrawResult();
                break;
            case OpCode.DisplayDraw:
                ReadDisplayDraw();
                break;
            case OpCode.Discard:
                ReadDiscard();
                break;
            case OpCode.DiscardResult:
                ReadDiscardResult();
                break;
            case OpCode.Swap:
                ReadSwap();
                break;
            case OpCode.DisplaySwap:
                ReadDisplaySwap();
                break;
            case OpCode.Peek:
                ReadPeek();
                break;
            case OpCode.PeekResult:
                ReadPeekResult();
                break;
            case OpCode.DisplayPeek:
                ReadDisplayPeek();
                break;
            case OpCode.Scramble:
                ReadScramble();
                break;
            case OpCode.DisplayScramble:
                ReadDisplayScramble();
                break;
            case OpCode.PassTurn:
                ReadPassTurn();
                break;
            case OpCode.ForceEndTurn:
                ReadForceEndTurn();
                break;
            case OpCode.CallIt:
                ReadCallIt();
                break;
            case OpCode.CalledIt:
                ReadCalledIt();
                break;
            case OpCode.GameEnd:
                ReadGameEnd();
                break;
            default:
                throw new InvalidOperationException("Attempted to process an unknown OpCode");
        }
    }

    #region ReadIndivdualPackets
    private void ReadAssignId()
    {
        _waitingPackets.Enqueue(new
            ProcessedAssignIdPacket(_senderId, ReadByte()));
    }

    private void ReadConnect()
    {
        _waitingPackets.Enqueue(new
            ProcessedConnectPacket(_senderId, ReadString()));
    }
    private void ReadConnectionResponse()
    {
        var success = ReadBoolean();
        if (success)
        {
            _waitingPackets.Enqueue(new
                ProcessedConnectionResponsePacket(_senderId, success, null));
            return;
        }

        var errorMessage = (ErrorMessage)ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedConnectionResponsePacket(_senderId, success, errorMessage));
    }
    private void ReadInitialiseGame()
    {
        var startingCards = ReadInt32();
        var deckSize = ReadInt32();
        var turnTimeLimit = ReadInt32();
        var playerCount = ReadByte();
        List<(byte, string)> players = [];
        for (var i = 0; i < playerCount; i++)
        {
            var id = ReadByte();
            players.Add((id, ReadString()));
        }

        _waitingPackets.Enqueue(new
            ProcessedInitialiseGamePacket(_senderId, startingCards, deckSize, turnTimeLimit, players));
    }

    private void ReadDisconnection()
    {
        var disconnectedPlayer = ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedDisconnectionPacket(_senderId, disconnectedPlayer));
    }

    private void ReadQuickPlace()
    {
        var cardId = ReadUInt16();
        var seenDiscard = (CardValue)ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedQuickPlacePacket(_senderId, cardId, seenDiscard));
    }

    private void ReadQuickPlaceResult()
    {
        var result = (QuickPlaceSuccess)ReadByte();
        var playerId = ReadByte();
        var cardValue = (CardValue)ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedQuickPlaceResultPacket(_senderId, result, playerId, cardValue));
    }

    private void ReadStartTurn()
    {
        var playerId = ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedStartTurnPacket(_senderId, playerId));
    }

    private void ReadDraw()
    {
        _waitingPackets.Enqueue(new
            ProcessedDrawPacket(_senderId));
    }

    private void ReadDrawResult()
    {
        var cardValue = (CardValue)ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedDrawResultPacket(_senderId, cardValue));
    }

    private void ReadDisplayDraw()
    {
        _waitingPackets.Enqueue(new
            ProcessedDisplayDrawPacket(_senderId));
    }

    private void ReadDiscard()
    {
        _waitingPackets.Enqueue(new
            ProcessedDiscardPacket(_senderId));
    }

    private void ReadDiscardResult()
    {
        var cardValue = (CardValue)ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedDiscardResultPacket(_senderId, cardValue));
    }

    private void ReadSwap()
    {
        var cardId1 = ReadUInt16();
        var cardId2 = ReadUInt16();
        _waitingPackets.Enqueue(new
            ProcessedSwapPacket(_senderId, cardId1, cardId2));
    }

    private void ReadDisplaySwap()
    {
        var cardId1 = ReadUInt16();
        var cardId2 = ReadUInt16();
        _waitingPackets.Enqueue(new
            ProcessedDisplaySwapPacket(_senderId, cardId1, cardId2));
    }

    private void ReadPeek()
    {
        var cardId = ReadUInt16();
        _waitingPackets.Enqueue(new
            ProcessedPeekPacket(_senderId, cardId));
    }

    private void ReadPeekResult()
    {
        var cardValue = (CardValue)ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedPeekResultPacket(_senderId, cardValue));
    }

    private void ReadDisplayPeek()
    {
        var cardId = ReadUInt16();
        _waitingPackets.Enqueue(new
            ProcessedDisplayPeekPacket(_senderId, cardId));
    }

    private void ReadScramble()
    {
        var playerId = ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedScramblePacket(_senderId, playerId));
    }

    private void ReadDisplayScramble()
    {
        var playerId = ReadByte();
        _waitingPackets.Enqueue(new
            ProcessedDisplayScramblePacket(_senderId, playerId));
    }

    private void ReadPassTurn()
    {
        _waitingPackets.Enqueue(new
            ProcessedPassTurnPacket(_senderId));
    }

    private void ReadForceEndTurn()
    {
        _waitingPackets.Enqueue(new
            ProcessedForceEndTurnPacket(_senderId));
    }

    private void ReadCallIt()
    {
        _waitingPackets.Enqueue(new
           ProcessedCallItPacket(_senderId));
    }

    private void ReadCalledIt()
    {
        _waitingPackets.Enqueue(new
            ProcessedCalledItPacket(_senderId));
    }

    private void ReadGameEnd()
    {
        List<(byte, int)> scores = [];
        var playerCount = ReadByte();
        for (var i = 0; i < playerCount; i++)
        {
            var playerId = ReadByte();
            var score = ReadInt32();
            scores.Add((playerId, score));
        }

        _waitingPackets.Enqueue(new
            ProcessedGameEndPacket(_senderId, scores));
    }

    #endregion

    private new string ReadString()
    {
        //byte[] byteStore;
        var messageLength = ReadInt32();
        //byteStore = new byte[messageLength];
        //_ = _stream.Read(byteStore, 0, messageLength);
        return Encoding.UTF8.GetString(ReadBytes(messageLength));
    }
}
