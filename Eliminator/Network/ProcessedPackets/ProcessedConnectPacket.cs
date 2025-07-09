namespace Eliminator.Network.ProcessedPackets;
public record ProcessedConnectPacket: IProcessedPacket
{
    public ProcessedConnectPacket(byte senderId, string username)
    {
        OpCode = OpCode.Connect;
        SenderId = senderId;
        Username = username;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public string Username { get; }
}
