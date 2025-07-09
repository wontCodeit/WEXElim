namespace Eliminator;
public interface IProcessedPacket
{
    public OpCode OpCode { get; }
    public byte SenderId { get; }
}
