namespace Pw.Hub.Relics.Shared.Packets;

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
