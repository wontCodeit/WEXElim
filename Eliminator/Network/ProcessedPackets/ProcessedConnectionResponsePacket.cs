namespace Eliminator.Network.ProcessedPackets;
public record ProcessedConnectionResponsePacket: IProcessedPacket
{
    public ProcessedConnectionResponsePacket(byte senderId, bool success, ErrorMessage? error)
    {
        OpCode = OpCode.ConnectionResponse;
        SenderId = senderId;
        Success = success;
        Error = error;
    }

    public OpCode OpCode { get; }

    public byte SenderId { get; }

    public bool Success { get; }

    public ErrorMessage? Error { get; }
}
