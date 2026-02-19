namespace Pw.Hub.Relics.Shared.Packets;

public abstract class BasePacket
{
    public int Type { get; }

    protected BasePacket(int type)
    {
        Type = type;
    }

    public abstract PacketStream Write(PacketStream stream);
    public abstract PacketStream Read(PacketStream stream);

    public virtual int PriorPolicy()
    {
        return 0;
    }

    public virtual bool SizePolicy(int size)
    {
        return true;
    }
}
