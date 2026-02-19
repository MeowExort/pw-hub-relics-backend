using System.Text;

namespace Pw.Hub.Relics.Shared.Packets;

public class DataStream : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryReader _reader;
    private readonly BinaryWriter _writer;
    private bool _disposed;

    public DataStream()
    {
        _stream = new MemoryStream();
        _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
    }

    public DataStream(byte[] data)
    {
        _stream = new MemoryStream(data);
        _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
    }

    public long Position => _stream.Position;
    public long Length => _stream.Length;

    public DataStream Read(ref int value)
    {
        value = _reader.ReadInt32();
        return this;
    }

    public DataStream Read(ref uint value)
    {
        value = _reader.ReadUInt32();
        return this;
    }

    public DataStream Read(ref short value)
    {
        value = _reader.ReadInt16();
        return this;
    }

    public DataStream Read(ref ushort value)
    {
        value = _reader.ReadUInt16();
        return this;
    }

    public DataStream Read(ref sbyte value)
    {
        value = _reader.ReadSByte();
        return this;
    }

    public DataStream Read(ref byte value)
    {
        value = _reader.ReadByte();
        return this;
    }

    public DataStream Read(ref long value)
    {
        value = _reader.ReadInt64();
        return this;
    }

    public DataStream Read(ref ulong value)
    {
        value = _reader.ReadUInt64();
        return this;
    }

    public DataStream Read<T>(ref T value) where T : IMarshalData, new()
    {
        value = new T();
        value.Read(this);
        return this;
    }

    public DataStream Read<T>(ref List<T> list) where T : IMarshalData, new()
    {
        int count = _reader.ReadInt32();
        list = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            var item = new T();
            item.Read(this);
            list.Add(item);
        }
        return this;
    }

    public DataStream Write(int value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(uint value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(short value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(ushort value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(sbyte value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(byte value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(long value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write(ulong value)
    {
        _writer.Write(value);
        return this;
    }

    public DataStream Write<T>(T value) where T : IMarshalData
    {
        value.Write(this);
        return this;
    }

    public DataStream Write<T>(List<T> list) where T : IMarshalData
    {
        _writer.Write(list.Count);
        foreach (var item in list)
        {
            item.Write(this);
        }
        return this;
    }

    public byte[] ToArray()
    {
        return _stream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reader.Dispose();
        _writer.Dispose();
        _stream.Dispose();
    }
}
