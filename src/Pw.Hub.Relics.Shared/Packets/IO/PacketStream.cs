namespace Pw.Hub.Relics.Shared.Packets.IO;

public class PacketStream : DataStream
{
    public PacketStream()
    {
        SetByteOrder(Endianness.BigEndian);
    }

    public PacketStream(IEnumerable<byte> data) : base(data)
    {
        SetByteOrder(Endianness.BigEndian);
    }
}