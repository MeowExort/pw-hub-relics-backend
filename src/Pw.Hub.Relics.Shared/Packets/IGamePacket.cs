namespace Pw.Hub.Relics.Shared.Packets;

public interface IGamePacket
{
    int Opcode { get; }
    int Length { get; }

    void SetType(int type);
    void SetSize(int size);

    int PriorPolicy();
    bool SizePolicy();
    bool SizePolicy(int size);
}