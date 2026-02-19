namespace Pw.Hub.Relics.Shared.Packets;

public class Relic : IMarshalData
{
    public int id = -1;
    public int exp = -1;
    public short main_addon = -1;
    public sbyte lock_ = -1;
    public sbyte reserve = -1;
    public List<RelicAddon> addons = [];

    public Relic() { }

    public DataStream Read(DataStream stream)
    {
        stream.Read(ref id);
        stream.Read(ref exp);
        stream.Read(ref main_addon);
        stream.Read(ref lock_);
        stream.Read(ref reserve);
        stream.Read(ref addons);

        return stream;
    }

    public DataStream Write(DataStream stream)
    {
        stream.Write(id);
        stream.Write(exp);
        stream.Write(main_addon);
        stream.Write(lock_);
        stream.Write(reserve);
        stream.Write(addons);

        return stream;
    }
}
