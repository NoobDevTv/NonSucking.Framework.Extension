using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using NonSucking.Framework.Serialization;

namespace NonSucking.Framework.Serialization
{
    class ThrowHelper
    {
#if NETSTANDARD2_1_OR_GREATER
        [DoesNotReturn]
#endif
        public static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();
    }

    public ref struct BinarySpanWriter
    {
        private readonly Span<byte> buffer;
        private int offset;

        private readonly Encoding encoding;

        public BinarySpanWriter(Span<byte> buffer)
            : this(buffer, Encoding.UTF8)
        {
        }

        public BinarySpanWriter(Span<byte> buffer, Encoding encoding)
        {
            this.buffer = buffer;
            offset = 0;
            this.encoding = encoding;
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }


        public void Write(byte value)
        {
            buffer[offset++] = value;
        }

        public void Write(byte[] buffer)
        {
            Write(new ReadOnlySpan<byte>(buffer));
        }

        public void Write(byte[] buffer, int index, int count)
        {
            Write(new ReadOnlySpan<byte>(buffer, index, count));
        }

        public void Write(char ch)
        {
#if NETSTANDARD2_1_OR_GREATER
            ReadOnlySpan<char> chars = stackalloc char[] { ch};
            var b = encoding.GetByteCount(chars);
            if (b != encoding.GetBytes(chars, buffer.Slice(offset)))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var chars = new [] { ch };
            var b = encoding.GetByteCount(chars);
            var res = encoding.GetBytes(chars);
            if (b != res.Length)
                ThrowHelper.ThrowIndexOutOfRange();
            res.CopyTo(buffer.Slice(offset));
#endif
        }

        public void Write(char[] chars)
        {
            Write(new ReadOnlySpan<char>(chars));
        }

        public void Write(char[] chars, int index, int count)
        {
            Write(new ReadOnlySpan<char>(chars, index, count));
        }

        public void Write(decimal value)
        {
#if NET5_0_OR_GREATER
            Span<int> buffer = stackalloc int[sizeof(decimal) / sizeof(int)];
            decimal.GetBits(value, buffer);
#else
            var buffer = decimal.GetBits(value);
#endif
            Write(buffer[0]);
            Write(buffer[1]);
            Write(buffer[2]);
            Write(buffer[3]);
        }

        
        public void Write(double value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
        }

        public void Write(short value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(short);
        }

        public void Write(int value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(int);
        }

        public void Write(long value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(long);
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            buffer.CopyTo(this.buffer.Slice(offset));
        }

        public void Write(ReadOnlySpan<char> chars)
        {
#if NETSTANDARD2_1_OR_GREATER
            var b = encoding.GetByteCount(chars);
            if (b != encoding.GetBytes(chars, buffer.Slice(offset)))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmp = chars.ToArray();
            var b = encoding.GetByteCount(tmp);
            var res = encoding.GetBytes(tmp);
            if (b != res.Length)
                ThrowHelper.ThrowIndexOutOfRange();
            res.CopyTo(buffer.Slice(offset));
#endif
        }

        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        public void Write(float value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(float);
        }

        public void Write(string value)
        {
            Write7BitEncodedInt(value.Length);
            Write(value.AsSpan());
        }

        public void Write(ushort value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(ushort);
        }

        public void Write(uint value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(uint);
        }

        public void Write(ulong value)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (!BitConverter.TryWriteBytes(this.buffer.Slice(offset), value))
                ThrowHelper.ThrowIndexOutOfRange();
#else
            var tmpBuffer = BitConverter.GetBytes(value);
            tmpBuffer.CopyTo(this.buffer.Slice(offset));
#endif
            offset += sizeof(ulong);
        }

        public void Write7BitEncodedInt(int value)
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

        public void Write7BitEncodedInt64(long value)
        {
            ulong uValue = (ulong)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }
    }
}