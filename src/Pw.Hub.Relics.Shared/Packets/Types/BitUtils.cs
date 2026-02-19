namespace Pw.Hub.Relics.Shared.Packets.Types;

public class BigEndian
{
}

public class LittleEndian
{
}

public class BitUtils<T>
{
    // https://referencesource.microsoft.com/#mscorlib/system/bitconverter.cs,9108fa2d0b37805b

    #region Params

    public static bool IsBig => typeof(T) == typeof(BigEndian);
    public static bool IsLittle => typeof(T) == typeof(LittleEndian);

    #endregion


    #region Int16

    public static unsafe short BytesToInt16(byte[] value, int offset = 0)
    {
        fixed (byte* pbyte = &value[offset])
        {
            if (IsBig)
                return (short)(*pbyte << 8 | *(pbyte + 1));

            if (IsLittle)
                return (short)(*pbyte | *(pbyte + 1) << 8);
        }

        return 0;
    }

    public static byte[] Int16ToBytes(short value)
    {
        if (IsBig)
        {
            return new[]
            {
                (byte)(value >> 8),
                (byte)value,
            };
        }

        if (IsLittle)
        {
            return new[]
            {
                (byte)value,
                (byte)(value >> 8),
            };
        }

        return new byte[0];
    }

    #endregion


    #region Int32

    public static unsafe int BytesToInt32(byte[] value, int offset = 0)
    {
        fixed (byte* pbyte = &value[offset])
        {
            if (IsBig)
                return *pbyte << 24 | *(pbyte + 1) << 16 | *(pbyte + 2) << 8 | *(pbyte + 3);

            if (IsLittle)
                return *pbyte | *(pbyte + 1) << 8 | *(pbyte + 2) << 16 | *(pbyte + 3) << 24;
        }

        return 0;
    }

    public static byte[] Int32ToBytes(int value)
    {
        if (IsBig)
        {
            return new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value,
            };
        }

        if (IsLittle)
        {
            return new[]
            {
                (byte)value,
                (byte)(value >> 8),
                (byte)(value >> 16),
                (byte)(value >> 24),
            };
        }

        return new byte[0];
    }

    #endregion


    #region Int64

    public static unsafe long BytesToInt64(byte[] value, int offset = 0)
    {
        fixed (byte* pbyte = &value[offset])
        {
            if (IsBig)
            {
                int i1 = *pbyte << 24 | *(pbyte + 1) << 16 | *(pbyte + 2) << 8 | *(pbyte + 3);
                int i2 = *(pbyte + 4) << 24 | *(pbyte + 5) << 16 | *(pbyte + 6) << 8 | *(pbyte + 7);
                return (uint)i2 | (long)i1 << 32;
            }

            if (IsLittle)
            {
                int i1 = *pbyte | *(pbyte + 1) << 8 | *(pbyte + 2) << 16 | *(pbyte + 3) << 24;
                int i2 = *(pbyte + 4) | *(pbyte + 5) << 8 | *(pbyte + 6) << 16 | *(pbyte + 7) << 24;
                return (uint)i1 | (long)i2 << 32;
            }
        }

        return 0;
    }

    public static byte[] Int64ToBytes(long value)
    {
        if (IsBig)
        {
            return new[]
            {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value,
            };
        }

        if (IsLittle)
        {
            return new[]
            {
                (byte)value,
                (byte)(value >> 8),
                (byte)(value >> 16),
                (byte)(value >> 24),
                (byte)(value >> 32),
                (byte)(value >> 40),
                (byte)(value >> 48),
                (byte)(value >> 56),
            };
        }

        return new byte[0];
    }

    #endregion


    #region Single

    public static unsafe float BytesToSingle(byte[] buffer, int offset = 0)
    {
        int result = BytesToInt32(buffer, offset);
        return *(float*)&result;
    }

    public static unsafe byte[] SingleToBytes(float value)
    {
        int result = *(int*)&value;
        return Int32ToBytes(result);
    }

    #endregion


    #region Double

    public static unsafe double BytesToDouble(byte[] buffer, int offset = 0)
    {
        long result = BytesToInt64(buffer, offset);
        return *(double*)&result;
    }

    public static unsafe byte[] DoubleToBytes(double value)
    {
        long result = *(long*)&value;
        return Int64ToBytes(result);
    }

    #endregion
}