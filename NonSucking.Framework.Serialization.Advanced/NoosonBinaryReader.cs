using System;
using System.IO;
using System.Text;

namespace NonSucking.Framework.Serialization;

public class NoosonBinaryReader : BinaryReader, IBinaryReader
{
    public NoosonBinaryReader(Stream input)
        : base(input)
    {
    }

    public NoosonBinaryReader(Stream input, Encoding encoding)
        : base(input, encoding)
    {
    }

    public NoosonBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        : base(input, encoding, leaveOpen)
    {
    }

#if NETSTANDARD2_0
    public int Read(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public int Read(Span<char> buffer)
    {
        throw new NotImplementedException();
    }
#endif
    public new int Read7BitEncodedInt()
        => base.Read7BitEncodedInt();

#if NET5_0_OR_GREATER
    public new long Read7BitEncodedInt64()
    {
        return base.Read7BitEncodedInt64();
    }
#else
    public long Read7BitEncodedInt64()
    {
        ulong result = 0;
        byte byteReadJustNow;

        // Read the integer 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        //
        // There are two failure cases: we've read more than 10 bytes,
        // or the tenth byte is about to cause integer overflow.
        // This means that we can read the first 9 bytes without
        // worrying about integer overflow.

        const int MaxBytesWithoutOverflow = 9;
        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            // ReadByte handles end of stream cases for us.
            byteReadJustNow = ReadByte();
            result |= (byteReadJustNow & 0x7Ful) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (long)result; // early exit
            }
        }

        // Read the 10th byte. Since we already read 63 bits,
        // the value of this byte must fit within 1 bit (64 - 63),
        // and it must not have the high bit set.

        byteReadJustNow = ReadByte();
        if (byteReadJustNow > 0b_1u)
        {
            throw new FormatException();
        }

        result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (long)result;
    }
#endif

    public void ReadBytes(Span<byte> buffer)
    {
        int read = 0;
        do
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var res = base.Read(buffer[read..]);
#else
            var tmpBuffer = new byte[buffer.Length - read];
            var res = base.Read(tmpBuffer, 0, tmpBuffer.Length);
            new Span<byte>(tmpBuffer, 0, read).CopyTo(buffer.Slice(read));
#endif
            if (res == -1)
                throw new EndOfStreamException();
            read += res;
        } while (read < buffer.Length);
    }
}