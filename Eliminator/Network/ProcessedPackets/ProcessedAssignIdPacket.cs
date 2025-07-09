namespace Eliminator.Network.ProcessedPackets;

public record ProcessedAssignIdPacket: IProcessedPacket
{
    public ProcessedAssignIdPacket(byte senderId, byte assignedId)
    {
        OpCode = OpCode.AssignId;
        SenderId = senderId;
        AssignId = assignedId;
    }

    public OpCode OpCode { get; }
    public byte SenderId { get; }
    public byte AssignId { get; }
}