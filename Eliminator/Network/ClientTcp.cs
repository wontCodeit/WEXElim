using Eliminator.Network.ProcessedPackets;
using System.Diagnostics;
using System.Net.Sockets;

namespace Eliminator;

/// <summary>
/// A class to facilitate TCP connections
/// </summary>
public class ClientTcp: IDisposable
{
    protected readonly TcpClient _tcpClient;
    private bool disposedValue;

    public bool HasBeenInitialised { get; private set; } = false;

    public byte Id { get; private set; } = 0;

    public string Username { get; private set; } = "Formless";

    public ClientTcp(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
    }

    /// <summary>
    /// Assign id to this <see cref="ClientTcp"/>. This should be done before sending any <see cref="IProcessedPacket"/> to the server
    /// </summary>
    /// <param name="id"> The id to assign </param>
    public void AssignId(byte id) => Id = id;

    public void AssignName(string username)
    {
        Debug.Assert(HasBeenInitialised is false, "ClientTcp has already been initialised!");
        Username = username;
        HasBeenInitialised = true;
    }

    public virtual void Start()
    {
        _ = Task.Run(Process);
    }

    public void SendPacket(byte[] packet)
    {
        _tcpClient.GetStream().Write(packet);
    }

    protected virtual void Process()
    {
        NetworkStream netStream = _tcpClient.GetStream();
        var reader = new PacketReader(netStream, Id);
        while (true)
        {
            try
            {
                var streamResult = netStream.ReadByte();
                if (streamResult != -1) // -1 is end of stream
                {
                    reader.Read((OpCode)streamResult);
                }
                else
                {
                    Console.WriteLine($"[{DateTime.UtcNow} {Username}:] EndOfStreamError occurred in Processing, attempting disconnect");
                    PacketReader.ReadInternalPacket(new ProcessedDisconnectionPacket(Id, Id));
                    break;
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"[{DateTime.UtcNow} {Username}:] Assumed intentional disconnect: {e.Message}");
                break;
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow} {Username}:] Assumed intentional disconnect.");
                break;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine($"[{DateTime.UtcNow} {Username}:] Error occurred in Processing, invalid OpCode: {e.Message}");
                // maybe should send some kind of warning packet to server/game handler here... with the power of TCP it shouldn't happen though
                // I am just going to make it obvious something went wrong for now
                PacketReader.ReadInternalPacket(new ProcessedDisconnectionPacket(Id, Id));
                break;
            }
        }

        Console.WriteLine($"[{DateTime.UtcNow} {Username}:] closed with id {Id}");
        _tcpClient.Close();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // No managed resources to clean up because the client interacts with sockets which are unmanaged
            }

            _tcpClient.Close(); // this breaks Process ending that task

            disposedValue = true;
        }
    }

    ~ClientTcp()
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
}