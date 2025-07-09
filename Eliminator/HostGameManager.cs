using Eliminator.Network.ProcessedPackets;

namespace Eliminator;

/// <summary>
/// Class that computes the logic and handles the packets of an Eliminator game
/// </summary>
public class HostGameManager
{
    private readonly HandManager _handManager;
    private readonly ServerTcp _server;
    private int _turnTimeLimit = 30;
    private List<byte> _playerIds = [];
    private bool _run = false;
    private byte _turnPlayerId = 0;
    private byte _callItPlayerId = 0;
    private bool _isCalled = false;
    private IEnumerable<CardAction> _expectedCardActions = [CardAction.None];
    private DateTime _currentTurnEndTime = DateTime.UtcNow;

    public HostGameManager(HandManager handManager, ServerTcp server)
    {
        _handManager = handManager;
        _server = server;
    }

    /// <summary>
    /// Connect and initialise all clients
    /// </summary>
    /// <param name="playerCount"> The number of connections to wait for </param>
    public void InitialiseAllClients(int playerCount)
    {
        List<string> clientNames = [];
        List<ClientTcp> newClients = _server.AcceptNewConnections(playerCount);
        newClients.ForEach(client => client.Start());
        Console.WriteLine($"[{DateTime.UtcNow} server:] Finished accepting connections");
        // TODO: change code so this sleep unnecessary i.e. there are retrying methods for missed/bad packets and responses
        Thread.Sleep(100); // Wait for every client to begin listening. If assign id packet missed, everything fails (Too bad!)
        for (byte i = 0; i < newClients.Count; i++)
        {
            _playerIds.Add(i);
            newClients[i].SendPacket(PacketWriter.WriteAssignIdPacket(i));
        }

        var responses = 0;
        while (responses < playerCount)
        {
            Console.WriteLine("Waiting on a packet...");
            IProcessedPacket nextPacket = PacketReader.GetNextPacket();
            Console.WriteLine($"Got a packet with OpCode: {nextPacket.OpCode}");
            var connectPacket = nextPacket as ProcessedConnectPacket;
            if (connectPacket == null)
            {
                Console.WriteLine("Unexpected packet received, only Connect packets wanted");
                continue;
            }

            if (clientNames.Contains(connectPacket.Username))
            {
                _server.BroadcastSpecific(
                    [connectPacket.SenderId],
                    PacketWriter.WriteConnectionResponsePacket(false, ErrorMessage.UsernameTaken));
                continue;
            }

            _server.BroadcastSpecific(
                [connectPacket.SenderId],
                PacketWriter.WriteConnectionResponsePacket(true, null));

            newClients.First(client => client.Id == connectPacket.SenderId).AssignName(connectPacket.Username);
            responses++;
        }

        Console.WriteLine($"[{DateTime.UtcNow} server:] Finished initialising clients");
    }

    /// <summary>
    /// Begin and run the game until completion
    /// </summary>
    /// <param name="turnTimeLimit"> How long in seconds each player is allowed to take on a turn </param>
    public void Run(int turnTimeLimit = 30)
    {
        _run = true;
        _server.BroadcastAll(
            PacketWriter.WriteInitialiseGamePacket(
                _handManager.InitialisationInfo.startingCards,
                _handManager.InitialisationInfo.deckSize,
                turnTimeLimit,
                _server.ConnectedPlayers())
            );
        Console.WriteLine("Sent initialise game packet");

        Thread.Sleep(1_000); // Wait for client game to start TODO: Remove by being more clever
        var r = new Random();
        _turnPlayerId = (byte)r.Next(0, _playerIds.Last());
        _server.BroadcastAll(
            PacketWriter.WriteStartTurnPacket(_turnPlayerId));
        Console.WriteLine("Sent start turn packet, id: " + _turnPlayerId);
        _expectedCardActions = [CardAction.DiscardSwap, CardAction.Draw, CardAction.QuickPlace];
        _turnTimeLimit = turnTimeLimit;
        _currentTurnEndTime = DateTime.UtcNow.AddSeconds(_turnTimeLimit);

        while (_run)
        {
            if (!PacketReader.NextPacketReady())
            {
                var result = _currentTurnEndTime.CompareTo(DateTime.UtcNow);
                if (result < 0) // less than zero indicates that current time exceeds end time
                {
                    PacketReader.ReadInternalPacket(new ProcessedForceEndTurnPacket(255));
                }
            }

            IProcessedPacket packet = PacketReader.GetNextPacket();
            switch (packet.OpCode)
            {
                //TODO: for every "Handle" method wherever it returns early the client should be notified of the problem
                case OpCode.Disconnection:
                    HandleDisconnection(packet as ProcessedDisconnectionPacket);
                    break;
                case OpCode.QuickPlace:
                    HandleQuickPlace(packet as ProcessedQuickPlacePacket);
                    break;
                case OpCode.PassTurn:
                    HandleEndTurn();
                    break;
                case OpCode.ForceEndTurn:
                    HandleEndTurn();
                    break;
                case OpCode.Draw:
                    HandleDraw(packet as ProcessedDrawPacket);
                    break;
                case OpCode.Discard:
                    HandleDiscard(packet as ProcessedDiscardPacket);
                    break;
                case OpCode.Swap:
                    HandleSwap(packet as ProcessedSwapPacket);
                    break;
                case OpCode.Peek:
                    HandlePeek(packet as ProcessedPeekPacket);
                    break;
                case OpCode.CallIt:
                    HandleCall(packet as ProcessedCallItPacket);
                    break;

                default:
                    // TODO: Probably need to send some synch packet or something here
                    Console.WriteLine($"[{DateTime.UtcNow} server:] unexpected packet");
                    Console.WriteLine($"[{DateTime.UtcNow} server:] received {packet.OpCode}");
                    continue;
            }
        }

        Card.Reset();
    }

    private void HandleCall(ProcessedCallItPacket? cPacket)
    {
        if (cPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (cPacket.SenderId != _turnPlayerId)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid CallIt request, request not from turn player");
            return;
        }

        if (_isCalled)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid CallIt request, already Called");
            return;
        }

        _isCalled = true;
        _callItPlayerId = _turnPlayerId;
    }

    private void HandleDraw(ProcessedDrawPacket? dPacket)
    {
        if (dPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (dPacket.SenderId != _turnPlayerId)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Draw request, request not from turn player");
            return;
        }

        if (!_expectedCardActions.Contains(CardAction.Draw))
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Draw request, does not match internal state");
            return;
        }

        if (_handManager.RemainingCards < 1)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Draw request, no cards remaining");
            return;
        }

        _expectedCardActions = [CardAction.Swap]; // TODO: mayhaps there should be additional checks and balances... with game state machine?
        _handManager.DrawCard();
        SendToWaitingPlayers(PacketWriter.WriteDisplayDrawPacket());
        _server.BroadcastSpecific(
            [_turnPlayerId],
            PacketWriter.WriteDrawResultPacket((CardValue)Card.GetNumber(_handManager.HeldCardId)!));
    }

    private void HandlePeek(ProcessedPeekPacket? pPacket)
    {
        if (pPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (pPacket.SenderId != _turnPlayerId)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Peek request, request not from turn player");
            return;
        }

        CardAction requestedAction =
            _handManager.GetCardsInHand(_turnPlayerId).Select(card => card.Id).Contains(pPacket.CardId)
            ? CardAction.PeekSelf
            : CardAction.PeekOther;

        if (!_expectedCardActions.Contains(requestedAction))
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Swap request, does not match internal state");
            return;
        }

        _expectedCardActions = [CardAction.None];
        _server.BroadcastSpecific(
            [_turnPlayerId],
            PacketWriter.WritePeekResultPacket((CardValue)Card.GetNumber(pPacket.CardId)!));
        SendToWaitingPlayers(PacketWriter.WriteDisplayPeekPacket(pPacket.CardId));
    }

    private void HandleSwap(ProcessedSwapPacket? sPacket)
    {
        if (sPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (sPacket.SenderId != _turnPlayerId)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Swap request, request not from turn player");
            return;
        }

        if (!_expectedCardActions.Contains(CardAction.Swap) ||
            !_expectedCardActions.Contains(CardAction.DiscardSwap))
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Swap request, does not match internal state");
            return;
        }

        var ownerId1 = GetCardOwnerId(sPacket.CardId1);
        var ownerId2 = GetCardOwnerId(sPacket.CardId2);

        // If this is a swap not with a server card, then it must not be in a locked player's hand
        if (ownerId1 != 255 || ownerId2 != 255)
        {
            var playerCard = ownerId1 < ownerId2 ? ownerId1 : ownerId2;
            if (!GetNonLockedPlayers().Contains(playerCard))
            {
                Console.WriteLine($"[{DateTime.UtcNow} server:] invalid QuickPlace request as card is from a locked player's hand");
                return;
            }
        }

        _handManager.Swap(sPacket.CardId1, sPacket.CardId2);
        _expectedCardActions = [CardAction.None];
        SendToWaitingPlayers(PacketWriter.WriteDisplaySwapPacket(sPacket.CardId1, sPacket.CardId2));
    }

    private void HandleDiscard(ProcessedDiscardPacket? dPacket)
    {
        if (dPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (dPacket.SenderId != _turnPlayerId)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid Discard request, request not from turn player");
            return;
        }

        _handManager.DiscardHeldCard();
        var discardedVal = (CardValue)Card.GetNumber(_handManager.TopDiscardCardId)!;
        _expectedCardActions = [discardedVal.GetCardAction()];
        _server.BroadcastSpecific([_turnPlayerId], PacketWriter.WriteDiscardResultPacket(discardedVal));
    }

    private void HandleEndTurn()
    {
        _turnPlayerId = GetNextPlayerId(_turnPlayerId);

        // End the game if it is called and the next player is the one who called it
        if (!_isCalled && _turnPlayerId != _callItPlayerId)
        {

            _server.BroadcastAll(PacketWriter.WriteStartTurnPacket(_turnPlayerId));
            _expectedCardActions = [CardAction.Swap];
            _currentTurnEndTime = DateTime.UtcNow.AddSeconds(_turnTimeLimit);
            return;
        }

        _server.BroadcastAll(PacketWriter.WriteGameEndPacket(_handManager.CalculateHandValues()));
        _run = false;
    }

    /// <summary>
    /// Handles a QuickPlace. Due to how punishment works (they get a card if they are wrong)
    /// you cannot QuickPlace when no cards are left in the deck. Perhaps there is some other solution but I haven't thought of one yet.
    /// Further, you can't quick place another player's card. I have always believed this is a foolish play and have removed it
    /// </summary>
    /// <param name="qpPacket"> The given <see cref="ProcessedQuickPlacePacket"/> to handle </param>
    private void HandleQuickPlace(ProcessedQuickPlacePacket? qpPacket)
    {
        if (qpPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (_handManager.RemainingCards < 1)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid QuickPlace request as no cards remain");
            return;
        }

        if (qpPacket.SenderId < _turnPlayerId && _isCalled)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid QuickPlace request as hand is locked");
            return;
        }

        if (!_handManager.GetCardsInHand(qpPacket.SenderId).Select(card => card.Id).Any(id => id == qpPacket.CardId))
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid QuickPlace request as card is from another player's hand");
            return;
        }

        if (!GetNonLockedPlayers().Contains(GetCardOwnerId(qpPacket.CardId)))
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] invalid QuickPlace request as card is from a locked player's hand");
            return;
        }

        // Need to get every client on the same page with what card is trying to be quick placed so it can be displayed
        SendToWaitingPlayers(PacketWriter.WriteQuickPlacePacket(qpPacket.CardId, qpPacket.SeenDiscard));

        var placedVal = (CardValue)Card.GetNumber(qpPacket.CardId)!;
        var success = _handManager.QuickPlace(qpPacket.CardId);
        if (success)
        {
            _server.BroadcastAll(PacketWriter.WriteQuickPlaceResultPacket
                (QuickPlaceSuccess.Success,
                qpPacket.SenderId,
                placedVal));
            _expectedCardActions = [placedVal.GetCardAction()];
            return;
        }

        if (placedVal == qpPacket.SeenDiscard)
        {
            _server.BroadcastAll(PacketWriter.WriteQuickPlaceResultPacket
                (QuickPlaceSuccess.TooLate,
                qpPacket.SenderId,
                placedVal));
            return;
        }

        _ = _handManager.PunishPlayer(qpPacket.SenderId); // Ids SHOULD be consistent across client and servers (always ticking up by one)
        _server.BroadcastAll(PacketWriter.WriteQuickPlaceResultPacket
            (QuickPlaceSuccess.Failure,
            qpPacket.SenderId,
            placedVal));
        return;
    }

    private void HandleDisconnection(ProcessedDisconnectionPacket? dcPacket)
    {
        if (dcPacket is null)
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] malformed packet");
            return;
        }

        if (_server.DisconnectClient(dcPacket.DisconnectedId))
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] disconnected-> {dcPacket.DisconnectedId}");
        }
        else
        {
            Console.WriteLine($"[{DateTime.UtcNow} server:] could not remove {dcPacket.DisconnectedId}");
        }

        _playerIds.Clear();
        _server.ConnectedPlayers().ForEach(client => _playerIds.Add(client.Item1));
        _playerIds = [.. _playerIds.OrderBy(id => id)];
        return;
    }

    private void SendToWaitingPlayers(byte[] packet)
    {
        _server.BroadcastSpecific(
            _playerIds.Where(id => id != _turnPlayerId),
            packet);
    }

    private byte GetCardOwnerId(ushort cardId)
    {
        byte cardOwnerId = 255; // server owned e.g. discard pile or held in waiting

        foreach (var pId in _playerIds)
        {
            List<Card> cards = _handManager.GetCardsInHand(pId);
            // have to do this strangeness because FirstOrDefault makes a Card instead of a null
            if (cards.Any(card => card.Id == cardId))
            {
                cardOwnerId = pId;
                break;
            }
        }

        return cardOwnerId;
    }

    private byte GetNextPlayerId(byte currentPlayerId)
    {
        return _playerIds.Contains((byte)(currentPlayerId + 1))
            ? (byte)(_turnPlayerId + 1)
            : _playerIds.First();
    }

    private IEnumerable<Byte> GetNonLockedPlayers()
    {
        if (!_isCalled)
        {
            return _playerIds;
        }

        List<Byte> playersInNewOrder = [_callItPlayerId];
        for (var i = 1; i < _playerIds.Count; i++)
        {
            playersInNewOrder.Add(GetNextPlayerId(playersInNewOrder[-1]));
        }

        var indexOfCurrentPlayer = playersInNewOrder.FindIndex(id => id == _turnPlayerId);
        try
        {
            return playersInNewOrder.Slice(indexOfCurrentPlayer, _playerIds.Count);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return [];
        }
    }
}
