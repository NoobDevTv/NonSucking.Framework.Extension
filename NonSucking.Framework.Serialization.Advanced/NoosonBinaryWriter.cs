using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NonSucking.Framework.Serialization;

public class NoosonBinaryWriter : System.IO.BinaryWriter, IBinaryWriter
{
    public NoosonBinaryWriter(Stream input)
        : base(input)
    {
    }

    public NoosonBinaryWriter(Stream input, Encoding encoding)
        : base(input, encoding)
    {
    }

    public NoosonBinaryWriter(Stream input, Encoding encoding, bool leaveOpen)
        : base(input, encoding, leaveOpen)
    {
    }
#if NETSTANDARD2_0
    public void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void Write(ReadOnlySpan<char> chars)
    {
        throw new NotImplementedException();
    }
#endif
    public new void Write7BitEncodedInt(int value)
        => base.Write7BitEncodedInt(value);

#if NET5_0_OR_GREATER
    public new void Write7BitEncodedInt64(long value)
    {
        base.Write7BitEncodedInt64(value);
    }
#else
    public void Write7BitEncodedInt64(long value)
    {
        uint uValue = (uint)value;

        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        while (uValue > 0x7Fu)
        {
            Write((byte)(uValue | ~0x7Fu));
            uValue >>= 7;
        }

        Write((byte)uValue);
    }
#endif

    public void WriteUnmanaged<T>(T value) where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(buffer, ref value);
        Write(buffer);
    }
}