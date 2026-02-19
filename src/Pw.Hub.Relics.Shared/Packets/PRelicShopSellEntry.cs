namespace Pw.Hub.Relics.Shared.Packets;

public class PRelicShopSellEntry : IMarshalData
{
    public PRelicShopLot sell_id = new();
    public int arrive_time = -1;
    public uint price = 0;
    public Relic relic_item = new();

    public PRelicShopSellEntry()
    {
    }

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