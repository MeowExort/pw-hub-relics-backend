using Pw.Hub.Relics.Shared.Packets.IO.Types;
using Pw.Hub.Relics.Shared.Packets.Protocol;

namespace Pw.Hub.Relics.Shared.Packets.IO;

public class DataStream : ByteOrder, IDisposable // TODO: add 'insert' methods (InsertBytes, InsertCUInt, ...)
{
    private readonly List<byte> _list = new();
    private int _position = 0;

    public byte[] Data
    {
        get => _list.ToArray();
    }

    public int Length
    {
        get => _list.Count;
    }

    public int Position
    {
        get => _position;
        set => _position = value;
    }

    public DataStream() { }
        
    public DataStream(byte[] data)
    {
        _list = new List<byte>(data);
    }

    public DataStream(List<byte> data)
    {
        _list = new List<byte>(data);
    }

    public DataStream(IEnumerable<byte> data)
    {
        _list = new List<byte>(data);
    }

    public DataStream(Octets data)
    {
        _list = new List<byte>(data.GetBytes());
    }

    public DataStream WriteCUInt(int value)
    {
        _list.AddRange(CUInt.Marshal(value));
        return this;
    }

    public int ReadCUInt()
    {
        CUInt.Unmarshal(_list, ref _position, out int value);
        return value;
    }

    public bool TryReadCUInt(ref int value)
    {
        int position = _position;
        return CUInt.Unmarshal(_list, ref position, out value);
    }

    public void WriteByte(Byte value)
    {
        _list.Add(value);
    }

    public void WriteSByte(SByte value)
    {
        _list.Add((byte)value);
    }

    public void WriteBytes(Byte[] value)
    {
        if (value == null || value.Length == 0)
            return;

        _list.AddRange(value);
    }

    public void WriteOctets(Octets value)
    {
        if (value is null)
        {
            WriteByte(0);
            return;
        }

        WriteCUInt(value.Length);
        Write(value.GetBytes());
    }

    public void WriteInt16(Int16 value)
    {
        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                WriteBytes(BitUtils<BigEndian>.Int16ToBytes(value));
            }
                break;
            case Endianness.LittleEndian:
            {
                WriteBytes(BitUtils<LittleEndian>.Int16ToBytes(value));
            }
                break;
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public void WriteInt32(Int32 value)
    {
        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                WriteBytes(BitUtils<BigEndian>.Int32ToBytes(value));
            }
                break;
            case Endianness.LittleEndian:
            {
                WriteBytes(BitUtils<LittleEndian>.Int32ToBytes(value));
            }
                break;
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public void WriteInt64(Int64 value)
    {
        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                WriteBytes(BitUtils<BigEndian>.Int64ToBytes(value));
            }
                break;
            case Endianness.LittleEndian:
            {
                WriteBytes(BitUtils<LittleEndian>.Int64ToBytes(value));
            }
                break;
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public void WriteUInt16(UInt16 value)
    {
        WriteInt16((Int16)value);
    }

    public void WriteUInt32(UInt32 value)
    {
        WriteInt32((Int32)value);
    }

    public void WriteUInt64(UInt64 value)
    {
        WriteInt64((Int64)value);
    }

    public void WriteSingle(Single value)
    {
        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                WriteBytes(BitUtils<BigEndian>.SingleToBytes(value));
            }
                break;
            case Endianness.LittleEndian:
            {
                WriteBytes(BitUtils<LittleEndian>.SingleToBytes(value));
            }
                break;
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public void WriteVector<T>(List<T> items) where T : IMarshalData
    {
        WriteCUInt(items.Count);

        foreach (var item in items)
        {
            if (item is IMarshalData data)
            {
                data.Write(this);
            }
        }
    }

    public IMarshalData WriteMarshalData<T>(T item) where T : IMarshalData
    {
        item.Write(this);
        return item;
    }


    public Byte ReadByte()
    {
        return ReadBytes(1)[0];
    }

    public SByte ReadSByte()
    {
        return (SByte)ReadBytes(1)[0];
    }

    public Byte[] ReadBytes(int length)
    {
        byte[] buffer = _list.GetRange(_position, length).ToArray();
        _position += length;
        return buffer;
    }

    public Octets ReadOctets(int? count = null)
    {
        int length = count ?? ReadCUInt();
        if (length == 0)
            return new Octets();

        return new Octets(ReadBytes(length));
    }

    public Int16 ReadInt16()
    {
        byte[] bytes = ReadBytes(2);

        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                return BitUtils<BigEndian>.BytesToInt16(bytes);
            }
            case Endianness.LittleEndian:
            {
                return BitUtils<LittleEndian>.BytesToInt16(bytes);
            }
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public Int32 ReadInt32()
    {
        byte[] bytes = ReadBytes(4);

        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                return BitUtils<BigEndian>.BytesToInt32(bytes);
            }
            case Endianness.LittleEndian:
            {
                return BitUtils<LittleEndian>.BytesToInt32(bytes);
            }
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public Int64 ReadInt64()
    {
        byte[] bytes = ReadBytes(8);

        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                return BitUtils<BigEndian>.BytesToInt64(bytes);
            }
            case Endianness.LittleEndian:
            {
                return BitUtils<LittleEndian>.BytesToInt64(bytes);
            }
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public UInt16 ReadUInt16()
    {
        return (UInt16)ReadInt16();
    }

    public UInt32 ReadUInt32()
    {
        return (UInt32)ReadInt32();
    }

    public UInt64 ReadUInt64()
    {
        return (UInt64)ReadInt64();
    }

    public Single ReadSingle()
    {
        byte[] bytes = ReadBytes(4);

        switch (Endianness)
        {
            case Endianness.BigEndian:
            {
                return BitUtils<BigEndian>.BytesToSingle(bytes);
            }
            case Endianness.LittleEndian:
            {
                return BitUtils<LittleEndian>.BytesToSingle(bytes);
            }
            default: throw new Exception("GameDataStream: Endianness nod valid");
        }
    }

    public List<T> ReadVector<T>(int count = 0) where T : IMarshalData, new()
    {
        if (count == 0)
            count = ReadCUInt();

        var list = new List<T>();

        for (int i = 0; i < count; i++)
        {
            if (new T() is IMarshalData data)
            {
                data.Read(this);
                list.Add((T)data);
            }
        }

        return list;
    }

    public IMarshalData ReadMarshalData<T>(T item) where T : IMarshalData
    {
        item.Read(this);
        return item;
    }

    public DataStream Write(Byte value)
    {
        WriteByte(value);
        return this;
    }

    public DataStream Write(SByte value)
    {
        WriteSByte(value);
        return this;
    }

    public DataStream Write(Byte[] value)
    {
        WriteBytes(value);
        return this;
    }

    public DataStream Write(Octets value)
    {
        WriteOctets(value);
        return this;
    }

    public DataStream Write(Int16 value)
    {
        WriteInt16(value);
        return this;
    }

    public DataStream Write(Int32 value)
    {
        WriteInt32(value);
        return this;
    }

    public DataStream Write(Int64 value)
    {
        WriteInt64(value);
        return this;
    }

    public DataStream Write(UInt16 value)
    {
        WriteUInt16(value);
        return this;
    }

    public DataStream Write(UInt32 value)
    {
        WriteUInt32(value);
        return this;
    }

    public DataStream Write(UInt64 value)
    {
        WriteUInt64(value);
        return this;
    }

    public DataStream Write(Single value)
    {
        WriteSingle(value);
        return this;
    }

    public DataStream Write<T>(List<T> value) where T : IMarshalData
    {
        WriteVector(value);
        return this;
    }

    public DataStream Write<T>(T value) where T : IMarshalData
    {
        WriteMarshalData(value);
        return this;
    }

    public DataStream Read(ref Byte value)
    {
        value = ReadByte();
        return this;
    }

    public DataStream Read(ref SByte value)
    {
        value = ReadSByte();
        return this;
    }

    public DataStream Read(ref Byte[] value, int length)
    {
        value = ReadBytes(length);
        return this;
    }

    public DataStream Read(ref Octets value, int? length = null)
    {
        value = ReadOctets(length);
        return this;
    }

    public DataStream Read(ref Int16 value)
    {
        value = ReadInt16();
        return this;
    }

    public DataStream Read(ref Int32 value)
    {
        value = ReadInt32();
        return this;
    }

    public DataStream Read(ref Int64 value)
    {
        value = ReadInt64();
        return this;
    }

    public DataStream Read(ref UInt16 value)
    {
        value = ReadUInt16();
        return this;
    }

    public DataStream Read(ref UInt32 value)
    {
        value = ReadUInt32();
        return this;
    }

    public DataStream Read(ref UInt64 value)
    {
        value = ReadUInt64();
        return this;
    }

    public DataStream Read(ref Single value)
    {
        value = ReadSingle();
        return this;
    }

    public DataStream Read<T>(ref List<T> value, int count = 0) where T : IMarshalData, new()
    {
        value = ReadVector<T>(count);
        return this;
    }

    public DataStream Read<T>(ref T value, int count = 0) where T : IMarshalData
    {
        value = (T)ReadMarshalData(value);
        return this;
    }

    public void Dispose()
    {
        _position = 0;
        _list.Clear();
    }
}