using System.Net.Sockets;

namespace Eliminator;

/// <summary>
/// A <see cref="ClientTcp"/> for use on client-side
/// I would like to make any client side accessors a singleton but I want 99% of the same functionality
/// </summary>
public class ServerAccess: ClientTcp
{
    public ServerAccess(TcpClient tcpClient) : base(tcpClient)
    {
    }

    protected override void Dispose(bool disposing)
    {
        SendPacket(PacketWriter.WriteDisconnectionPacket(Id));
        base.Dispose(disposing);
    }
}
