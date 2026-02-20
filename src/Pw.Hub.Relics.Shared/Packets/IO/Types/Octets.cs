using System.Text;

namespace Pw.Hub.Relics.Shared.Packets.IO.Types;

public class Octets
{
    private readonly byte[] m_data = [];

    public int Size => _GetLength();
    public int Length => _GetLength();

    public int GetSize() => _GetLength();
    public int GetLength() => _GetLength();

    public Octets()
    {
        m_data = [];
    }

    public Octets(ReadOnlySpan<byte> data)
    {
        m_data = data.ToArray();
    }

    public Octets(string data, Encoding encoding)
    {
        m_data = encoding.GetBytes(data);
    }

    private int _GetLength()
    {
        return m_data?.Length ?? 0;
    }

    public byte[] GetBytes()
    {
        return m_data ?? null;
    }

    public string GetString(string _arg_encoding)
    {
        var encoding = Encoding.GetEncoding(_arg_encoding);
        if (encoding == null)
            return string.Empty;
        return GetString(encoding);
    }

    public string GetString(Encoding _arg_encoding = null)
    {
        if (m_data == null)
            return string.Empty;
        return _arg_encoding?.GetString(m_data) ?? Encoding.ASCII.GetString(m_data);
    }

    public static implicit operator Octets(byte[] data)
    {
        return new Octets(data);
    }

    public static explicit operator byte[] (Octets data)
    {
        return data.GetBytes();
    }

    public string ToHex()
    {
        return BitConverter.ToString(m_data);
    }

    public string ToHexString(bool withoutSymbols = false, bool lower = false)
    {
        var hexString = BitConverter.ToString(m_data);

        if (withoutSymbols)
            hexString = hexString.Replace("-", "");

        if (lower)
            hexString = hexString.ToLower();

        return hexString;
    }
}