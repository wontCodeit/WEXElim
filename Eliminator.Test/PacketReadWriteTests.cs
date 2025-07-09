using Eliminator.Network.ProcessedPackets;
using System.Net;
using System.Net.Sockets;

namespace Eliminator.Test;
public class PacketReadWriteTests
{
    private static readonly Socket _sock = CreateBoundAndConnectedSocket();

    private static Socket CreateBoundAndConnectedSocket()
    {
        var sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
        sock.Bind(new IPEndPoint(IPAddress.Any, 80));
        sock.Connect(IPAddress.Parse("127.0.0.1"), 80);
        return sock;
    }

    /// <summary>
    /// GIVEN: An existing connection
    /// WHEN: A connect packet is sent
    /// THEN: The packet may be read and all sent information retrieved accurately
    /// </summary>
    [Fact]
    public void ConnectionPacketTest()
    {
        // ARRANGE
        var packet = PacketWriter.WriteConnectionPacket("Jemima Banks");
        var netStream = new NetworkStream(_sock);
        var reader = new PacketReader(netStream, 255); // 255 will be from server
        _ = _sock.Send(packet);
        var packetOpCode = (OpCode)reader.ReadByte();

        // ACT
        reader.Read(packetOpCode);

        // ASSERT
        IProcessedPacket processedPacket = PacketReader.GetNextPacket();
        Assert.True(OpCode.Connect == processedPacket.OpCode);
        Assert.True(255 == processedPacket.SenderId);
        var connectPacket = processedPacket as ProcessedConnectPacket;
        Assert.NotNull(connectPacket);
        Assert.True(connectPacket.Username == "Jemima Banks");
    }

    /// <summary>
    /// GIVEN: An existing connection
    /// WHEN: A successful connection response packet is sent
    /// THEN: The packet may be read and all sent information retrieved accurately
    /// </summary>
    [Fact]
    public void ConnectionResponseSuccessPacketTest()
    {
        // ARRANGE
        var netStream = new NetworkStream(_sock);
        var reader = new PacketReader(netStream, 255); // 255 will be from server
        var packet = PacketWriter.WriteConnectionResponsePacket(true, null);
        _ = _sock.Send(packet);
        var packetOpCode = (OpCode)reader.ReadByte();

        // ACT
        reader.Read(packetOpCode);

        // ASSERT
        IProcessedPacket processedPacket = PacketReader.GetNextPacket();
        Assert.True(OpCode.ConnectionResponse == processedPacket.OpCode);
        Assert.True(255 == processedPacket.SenderId);
        var unboxedPacket = processedPacket as ProcessedConnectionResponsePacket;
        Assert.NotNull(unboxedPacket);
        Assert.True(unboxedPacket.Success);
        Assert.Null(unboxedPacket.Error);
    }

    /// <summary>
    /// GIVEN: An existing connection
    /// WHEN: A failed connection response packet is sent
    /// THEN: The packet may be read and all sent information retrieved accurately
    /// </summary>
    [Fact]
    public void ConnectionResponseFailedPacketTest()
    {
        // ARRANGE
        var netStream = new NetworkStream(_sock);
        var reader = new PacketReader(netStream, 255); // 255 will be from server
        var packet = PacketWriter.WriteConnectionResponsePacket(false, ErrorMessage.UsernameTaken);
        _ = _sock.Send(packet);
        var packetOpCode = (OpCode)reader.ReadByte();

        // ACT
        reader.Read(packetOpCode);

        // ASSERT
        IProcessedPacket processedPacket = PacketReader.GetNextPacket();
        Assert.True(OpCode.ConnectionResponse == processedPacket.OpCode);
        Assert.True(255 == processedPacket.SenderId);
        var unboxedPacket = processedPacket as ProcessedConnectionResponsePacket;
        Assert.NotNull(unboxedPacket);
        Assert.False(unboxedPacket.Success);
        Assert.True(unboxedPacket.Error == ErrorMessage.UsernameTaken);
    }

    /// <summary>
    /// GIVEN: An existing connection
    /// WHEN: An initialise game packet is sent
    /// THEN: The packet may be read and all sent information retrieved accurately
    /// </summary>
    [Fact]
    public void InitialiseGamePacketTest()
    {
        // ARRANGE
        var netStream = new NetworkStream(_sock);
        var reader = new PacketReader(netStream, 255); // 255 will be from server
        var startCards = 4;
        var deckSize = 1;
        var turnTimeLimit = 30;
        List<(byte, string)> players = [(0, "Jemima Banks"), (1, "Kaladin"), (2, "Kelsier"), (3, "Kalak"), (4, "K2")];
        var packet = PacketWriter.WriteInitialiseGamePacket(startCards, deckSize, turnTimeLimit, players);
        _ = _sock.Send(packet);
        var packetOpCode = (OpCode)reader.ReadByte();

        // ACT
        reader.Read(packetOpCode);

        // ASSERT
        IProcessedPacket processedPacket = PacketReader.GetNextPacket();
        Assert.True(OpCode.InitialiseGame == processedPacket.OpCode);
        Assert.True(255 == processedPacket.SenderId);
        var unboxedPacket = processedPacket as ProcessedInitialiseGamePacket;
        Assert.NotNull(unboxedPacket);
        Assert.Equal(startCards, unboxedPacket.StartingCards);
        Assert.Equal(deckSize, unboxedPacket.DeckSize);
        Assert.Equal(turnTimeLimit, unboxedPacket.TurnTimeLimit);
        Assert.Equal(players, unboxedPacket.Players);
    }

    /// <summary>
    /// GIVEN: An existing connection
    /// WHEN: A disconnection packet is sent
    /// THEN: The packet may be read and all sent information retrieved accurately
    /// </summary>
    [Fact]
    public void DisconnectionTest()
    {
        // ARRANGE
        var netStream = new NetworkStream(_sock);
        var reader = new PacketReader(netStream, 255); // 255 will be from server
        var packet = PacketWriter.WriteDisconnectionPacket(1);
        _ = _sock.Send(packet);
        var packetOpCode = (OpCode)reader.ReadByte();

        // ACT
        reader.Read(packetOpCode);

        // ASSERT
        IProcessedPacket processedPacket = PacketReader.GetNextPacket();
        Assert.True(OpCode.Disconnection == processedPacket.OpCode);
        Assert.True(255 == processedPacket.SenderId);
        var unboxedPacket = processedPacket as ProcessedDisconnectionPacket;
        Assert.NotNull(unboxedPacket);
        Assert.Equal(1, unboxedPacket.DisconnectedId);
    }

    /// <summary>
    /// GIVEN: An existing connection
    /// WHEN: A quick place packet is sent
    /// THEN: The packet may be read and all sent information retrieved accurately
    [Fact]
    public void QuickPlaceTest()
    {
        // ARRANGE
        var netStream = new NetworkStream(_sock);
        var reader = new PacketReader(netStream, 255); // 255 will be from server
        var packet = PacketWriter.WriteQuickPlacePacket(0, CardValue.SpadesAce);
        _ = _sock.Send(packet);
        var packetOpCode = (OpCode)reader.ReadByte();

        // ACT
        reader.Read(packetOpCode);

        // ASSERT
        IProcessedPacket processedPacket = PacketReader.GetNextPacket();
        Assert.True(OpCode.QuickPlace == processedPacket.OpCode);
        Assert.True(255 == processedPacket.SenderId);
        var unboxedPacket = processedPacket as ProcessedQuickPlacePacket;
        Assert.NotNull(unboxedPacket);
        Assert.Equal(0, unboxedPacket.CardId);
        Assert.True(CardValue.SpadesAce == unboxedPacket.SeenDiscard);
    }

    // I don't think there is any value in more tests atm
}