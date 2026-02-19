namespace Pw.Hub.Relics.Shared.Packets;

public interface IMarshalData
{
    DataStream Read(DataStream stream);
    DataStream Write(DataStream stream);
}
