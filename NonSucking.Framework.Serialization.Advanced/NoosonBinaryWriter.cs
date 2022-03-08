using System;

namespace NonSucking.Framework.Serialization;

public class NoosonBinaryWriter : System.IO.BinaryWriter, IBinaryWriter
{
#if NETSTANDARD2_0
    public void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotImplementedException();
    }
#endif

#if NETSTANDARD2_0
    public void Write(ReadOnlySpan<char> chars)
    {
        throw new NotImplementedException();
    }
#endif
    public new void Write7BitEncodedInt(int value)
        => base.Write7BitEncodedInt(value);

    public void Write7BitEncodedInt64(long value)
    {
#if NET5_OR_GREATER
        base.Write7BitEncodedInt64(value);
#else
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
#endif
    }
}