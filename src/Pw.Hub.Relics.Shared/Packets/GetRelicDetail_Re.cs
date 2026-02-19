namespace Pw.Hub.Relics.Shared.Packets;

public class GetRelicDetail_Re : BasePacket
{
    public int retcode = -1;
    public List<PRelicShopSellEntry> lots = [];

    public GetRelicDetail_Re() : base(type: 6320)
    {
    }

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