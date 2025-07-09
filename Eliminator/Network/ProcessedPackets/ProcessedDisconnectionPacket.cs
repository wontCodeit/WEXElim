namespace Eliminator.Network.ProcessedPackets;

public record ProcessedDisconnectionPacket: IProcessedPacket
{
    public ProcessedDisconnectionPacket(byte sender, byte disconnectedId)
    {
        OpCode = OpCode.Disconnection;
        SenderId = sender;
        DisconnectedId = disconnectedId;
    }

    public OpCode OpCode { get; }

    public byte SenderId { get; }

    public byte DisconnectedId { get; }
}