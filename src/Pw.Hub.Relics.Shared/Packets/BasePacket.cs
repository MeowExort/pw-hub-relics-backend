namespace Pw.Hub.Relics.Shared.Packets;

public class BasePacket : IGamePacket
{
    private int p_Type { get; set; }
    private int p_Size { get; set; }

    public int Opcode { get => p_Type; }
    public int Length { get => p_Size; }

    public BasePacket() { }
    public BasePacket(int type, int size = 0)
    {
        p_Type = type;
        p_Size = size;
    }

    public void SetType(int type) { p_Type = type; }
    public void SetSize(int size) { p_Size = size; }

    public virtual PacketStream Write(PacketStream stream)
    {
        return stream;
    }

    public virtual PacketStream Read(PacketStream stream)
    {
        return stream;
    }

    public virtual int PriorPolicy() { return 0; }
    public virtual bool SizePolicy(int size) { return false; }
    public virtual bool SizePolicy() { return SizePolicy(p_Size); }
}