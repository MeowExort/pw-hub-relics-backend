using Pw.Hub.Relics.Shared.Packets.Types;

namespace Pw.Hub.Relics.Shared.Packets;

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
