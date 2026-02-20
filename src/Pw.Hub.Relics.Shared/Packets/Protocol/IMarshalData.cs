using Pw.Hub.Relics.Shared.Packets.IO;

namespace Pw.Hub.Relics.Shared.Packets.Protocol;

public interface IMarshalData
{
    DataStream Write(DataStream stream);
    DataStream Read(DataStream stream);
}