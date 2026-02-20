using Pw.Hub.Relics.Shared.Packets.IO;
using Pw.Hub.Relics.Shared.Packets.Protocol;

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

public class RelicAddon : IMarshalData
{
    public short id = -1;
    public int Id => id;
    public short value = -1;
    public int Value => value;

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

public class PRelicShopLot : IMarshalData
{
    // int64_t sell_id;
    public int player_id = -1;
    public int pos_in_shop = -1;
    // end of int64_t sell_id;

    public PRelicShopLot() { }

    public DataStream Read(DataStream stream)
    {
        stream.Read(ref player_id);
        stream.Read(ref pos_in_shop);

        return stream;
    }

    public DataStream Write(DataStream stream)
    {
        stream.Write(player_id);
        stream.Write(pos_in_shop);

        return stream;
    }
}
public class PRelicShopSellEntry : IMarshalData
{
    public PRelicShopLot sell_id = new();
    public int arrive_time = -1;
    public uint price = 0;
    public Relic relic_item = new();

    public PRelicShopSellEntry() { }

    public DataStream Read(DataStream stream)
    {
        stream.Read(ref sell_id);
        stream.Read(ref arrive_time);
        stream.Read(ref price);
        stream.Read(ref relic_item);

        return stream;
    }

    public DataStream Write(DataStream stream)
    {
        stream.Write(sell_id);
        stream.Write(arrive_time);
        stream.Write(price);
        stream.Write(relic_item);

        return stream;
    }
}
public class GetRelicDetail_Re : BasePacket
{
    public int retcode = -1;
    public List<PRelicShopSellEntry> lots = [];

    public GetRelicDetail_Re() : base(type: 6320) { }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(retcode);
        stream.Write(lots);

        return stream;
    }

    public override PacketStream Read(PacketStream stream)
    {
        stream.Read(ref retcode);
        stream.Read(ref lots);

        return stream;
    }

    public override int PriorPolicy()
    {
        return 1;
    }

    public override bool SizePolicy(int size)
    {
        return size <= 32768;
    }
}
