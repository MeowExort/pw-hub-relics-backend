namespace Pw.Hub.Relics.Shared.Packets.Types;

public static class CUInt
{
    #region compact_uint32

    public static Byte[] Marshal(Int32 value) => Marshal((UInt32)value);

    public static Byte[] Marshal(UInt32 value)
    {
        if (value < 0x80) return new Byte[] { (Byte)value };
        if (value < 0x4000) return BitConverter.GetBytes(byteorder_16((UInt16)(value | 0x8000)));
        if (value < 0x20000000) return BitConverter.GetBytes(byteorder_32(value | 0xc0000000));
        return new Byte[] { 0xe0 }.Concat(BitConverter.GetBytes(byteorder_32(value))).ToArray();
    }

    #endregion

    #region uncompact_uint32

    public static bool Unmarshal(List<Byte> buffer, ref Int32 pos, out Int32 value)
    {
        bool result = Unmarshal(buffer, ref pos, out uint temp_value);
        value = (Int32)temp_value;
        return result;
    }

    public static bool Unmarshal(Byte[] buffer, ref Int32 pos, out Int32 value)
    {
        bool result = Unmarshal(buffer, ref pos, out uint temp_value);
        value = (Int32)temp_value;
        return result;
    }

    public static bool Unmarshal(List<Byte> buffer, ref Int32 pos, out UInt32 value) =>
        Unmarshal(buffer.ToArray(), ref pos, out value);

    public static bool Unmarshal(Byte[] buffer, ref Int32 pos, out UInt32 value)
    {
        value = 0;

        if (buffer.eos(pos + 1))
            return false;

        switch (buffer[pos] & 0xe0)
        {
            case 0xe0:
                if (buffer.eos(pos + 5))
                    return false;
                pos += 1;
                value = byteorder_32(BitConverter.ToUInt32(buffer, pos));
                pos += 4;
                return true;

            case 0xc0:
                if (buffer.eos(pos + 4))
                    return false;
                value = byteorder_32(BitConverter.ToUInt32(buffer, pos)) & ~0xc0000000;
                pos += 4;
                return true;

            case 0xa0:
            case 0x80:
                if (buffer.eos(pos + 2))
                    return false;
                value = (UInt16)(byteorder_16(BitConverter.ToUInt16(buffer, pos)) & ~0x8000);
                pos += 2;
                return true;
        }

        value = buffer[pos];
        pos += 1;
        return true;
    }

    #endregion

    private static bool eos(this Byte[] buffer, Int32 position) => position > buffer.Length;

    private static UInt16 byteorder_16(UInt16 x) => (UInt16)(((x & 0x000000FF) << 8) + (x >> 8 & 0x000000FF));

    private static UInt32 byteorder_32(UInt32 x) => ((x & 0x000000FF) << 24) + ((x >> 8 & 0x000000FF) << 16) +
                                                    ((x >> 16 & 0x000000FF) << 8) + (x >> 24 & 0x000000FF);
}