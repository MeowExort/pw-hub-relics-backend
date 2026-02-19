namespace Pw.Hub.Relics.Shared.Packets;

public class RelicAddon : IMarshalData
{
    public int id = -1;
    public int value = -1;

    public RelicAddon() { }

    public DataStream Read(DataStream stream)
    {
        stream.Read(ref id);
        stream.Read(ref value);
        return stream;
    }

    public DataStream Write(DataStream stream)
    {
        stream.Write(id);
        stream.Write(value);
        return stream;
    }
}
