using System.Net;
using System.Net.Sockets;

namespace Eliminator;
public class ServerTcp
{
    private static bool _initialised = false;
    private static ServerTcp? _instance;
    private static int _port = 80;
    private static IPAddress _ipAddress = IPAddress.Parse("127.0.0.1");

    private readonly TcpListener _listener;
    private readonly List<ClientTcp> _clients = [];

    public static ServerTcp Instance
    {
        get
        {
            if (!_initialised)
            {
                throw new InvalidOperationException("Must initialise first");
            }

            _instance ??= new ServerTcp();

            return _instance;
        }
    }

    public static void InitialiseDns(int port)
    {
        _initialised = true;
        _port = port;
    }

    public static void InitialisePortForwarded(int port, IPAddress iPAddress)
    {
        _initialised = true;
        _port = port;
        _ipAddress = iPAddress;
    }

    private ServerTcp()
    {
        _listener = new(_ipAddress, _port);

        Console.WriteLine(_listener?.LocalEndpoint.ToString());
        _listener!.Start();
    }

    public void BroadcastAll(byte[] packet)
    {
        _clients.ForEach(client =>
        {
            client.SendPacket(packet);
        });
    }

    public void BroadcastSpecific(IEnumerable<byte> clientIds, byte[] packet)
    {
        _clients.ForEach(client =>
        {
            if (clientIds.Contains(client.Id))
            {
                client.SendPacket(packet);
            }
        });
    }

    public List<ClientTcp> AcceptNewConnections(int expectedPlayerCount)
    {
        while (_clients.Count < expectedPlayerCount)
        {
            Console.WriteLine("Waiting on a client...");
            _clients.Add(new ClientTcp(_listener.AcceptTcpClient()));
        }

        return _clients;
    }

    /// <summary>
    /// Attempts to dispose of and remove from internal client list the client with the given Id
    /// </summary>
    /// <param name="id"> Id of the client that should be disposed of </param>
    /// <returns> Whether the client was successfully disposed </returns>
    public bool DisconnectClient(byte id)
    {
        ClientTcp? clientToRemove = _clients.FirstOrDefault(client => client.Id == id);
        if (clientToRemove is null)
        {
            return false;
        }

        if (_clients.Remove(clientToRemove))
        {
            clientToRemove.Dispose();
            return true;
        }

        return false;
    }

    public List<(byte, string)> ConnectedPlayers()
    {
        var clientInfo = new List<(byte, string)>();
        _clients.ForEach(client => clientInfo.Add((client.Id, client.Username)));

        return clientInfo;
    }
}
