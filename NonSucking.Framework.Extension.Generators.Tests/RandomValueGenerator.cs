using System;
using NonSucking.Framework.Serialization;

namespace DEMO;

public class RandomValueGenerator : IBinaryReader
{
    private readonly Random random;

    public RandomValueGenerator(int seed)
    {
        random = new Random(seed);
    }

    public int PeekChar()
    {
        throw new NotSupportedException();
    }

    public int Read()
    {
        return ReadChar();
    }

    public int Read(byte[] buffer, int index, int count)
    {
        return Read(new Span<byte>(buffer, index, count));
    }

    public int Read(char[] buffer, int index, int count)
    {
        return Read(new Span<char>(buffer, index, count));
    }

    public int Read(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = ReadByte();
        }
        return buffer.Length;
    }

    public int Read(Span<char> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = ReadChar();
        }

        return buffer.Length;
    }

    public int Read7BitEncodedInt()
    {
        return ReadInt32();
    }

    public long Read7BitEncodedInt64()
    {
        return ReadInt64();
    }

    public bool ReadBoolean()
    {
        return random.Next(0, 2) == 0;
    }

    public byte ReadByte()
    {
        return (byte)random.Next(byte.MinValue, byte.MaxValue + 1);
    }

    public byte[] ReadBytes(int count)
    {
        var buffer = new byte[count];
        Read(buffer);
        return buffer;
    }

    public char ReadChar()
    {
        return (char)random.Next(char.MinValue, char.MaxValue + 1);
    }

    public char[] ReadChars(int count)
    {
        var buffer = new char[count];
        Read(buffer);
        return buffer;
    }

    public decimal ReadDecimal()
    {
        return new decimal(random.Next(), random.Next(), random.Next(), ReadBoolean(), (byte)random.Next(0, 28 + 1));
    }

    public double ReadDouble()
    {
        return random.NextDouble();
    }

    public short ReadInt16()
    {
        return (short)random.Next(short.MinValue, short.MaxValue + 1);
    }

    public int ReadInt32()
    {
        return random.Next();
    }

    public long ReadInt64()
    {
        return random.NextInt64();
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    public float ReadSingle()
    {
        return (float)ReadDouble();
    }

    public string ReadString()
    {
        var len = random.Next(0, 512);
        return new string(ReadChars(len));
    }

    public ushort ReadUInt16()
    {
        return (ushort)ReadInt16();
    }

    public uint ReadUInt32()
    {
        return (uint)ReadInt32();
    }

    public ulong ReadUInt64()
    {
        return (ulong)ReadInt64();
    }

    public void ReadBytes(Span<byte> buffer)
    {
        Read(buffer);
    }
}