namespace Pw.Hub.Relics.Shared.Packets.Types;

public class ByteOrder
{
    private Endianness m_byteorder { get; set; }

    public Endianness Endian => m_byteorder;
    public Endianness Endianness => m_byteorder;

    public bool IsBigEndian => m_byteorder == Endianness.BigEndian;
    public bool IsLittleEndian => m_byteorder == Endianness.LittleEndian;

    public ByteOrder() => SetByteOrder(Endianness.LittleEndian);

    public Endianness GetEndian() => m_byteorder;
    public Endianness GetEndianness() => m_byteorder;
    public Endianness GetByteOrder() => m_byteorder;

    public void SetBigEndian() => m_byteorder = Endianness.BigEndian;
    public void SetLittleEndian() => m_byteorder = Endianness.LittleEndian;

    public void SetEndian(string value) => _parse(value);
    public void SetEndianness(string value) => _parse(value);
    public void SetByteOrder(string value) => _parse(value);

    public void SetEndian(Endianness value) => _parse(value.ToString());
    public void SetEndianness(Endianness value) => _parse(value.ToString());
    public void SetByteOrder(Endianness value) => _parse(value.ToString());

    public void Reverse() => _reverse();
    public void ReverseEndian() => _reverse();
    public void ReverseEndianness() => _reverse();
    public void ReverseByteOrder() => _reverse();

    private void _reverse()
    {
        switch (GetEndianness())
        {
            case Endianness.BigEndian: SetLittleEndian(); break;
            case Endianness.LittleEndian: SetBigEndian(); break;
        }
    }

    private void _parse(string value)
    {
        switch (value.ToLower())
        {
            case "big":
            case "bigendian":
            case "bigendianness":
            case "big-endian":
            case "big-endianness":
                SetBigEndian();
                break;
            case "little":
            case "littleendian":
            case "littleendianness":
            case "little-endian":
            case "little-endianness":
                SetLittleEndian();
                break;
        }
    }
}