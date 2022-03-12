using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NonSucking.Framework.Serialization
{
    public ref struct BinarySpanReader
    {
        private readonly ReadOnlySpan<byte> buffer;
        private int offset;
        private readonly bool _2BytesPerChar;
        private const int MaxCharBytesSize = 128;
        private byte[]? charBytes;
        private readonly Decoder decoder;
        private readonly Encoding encoding;

        public BinarySpanReader(ReadOnlySpan<byte> buffer)
            : this(buffer, Encoding.UTF8)
        {
        }

        public BinarySpanReader(ReadOnlySpan<byte> buffer, Encoding encoding)
        {
            this.buffer = buffer;
            offset = 0;
            _2BytesPerChar = encoding is UnicodeEncoding;
            this.encoding = encoding;
            decoder = encoding.GetDecoder();
            charBytes = null;
            encoding.GetMaxCharCount(MaxCharBytesSize);
        }

        public int PeekChar()
        {
            var origPos = offset;
            var ch = Read();
            offset = origPos;
            return ch;
        }

        public int Read()
        {
            int charsRead = 0;
            int numBytes;
            int posSav = offset;

            charBytes ??= new byte[MaxCharBytesSize];

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            Span<char> singleChar = stackalloc char[1];
#else
            var singleChar = new char[1];
#endif
            while (charsRead == 0)
            {
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                // Assume 1 byte can be 1 char unless _2BytesPerChar is true.
                numBytes = _2BytesPerChar ? 2 : 1;

                if (offset >= buffer.Length)
                {
                    numBytes = 0;
                    charBytes[0] = unchecked((byte)-1);
                }
                else
                {
                    charBytes[0] = ReadByte();
                }

                if (numBytes == 2)
                {
                    if (offset >= buffer.Length)
                    {
                        numBytes = 1;
                        charBytes[1] = unchecked((byte)-1);
                    }
                    else
                    {
                        charBytes[1] = ReadByte();
                    }
                }

                if (numBytes == 0)
                {
                    return -1;
                }

                Debug.Assert(numBytes is 1 or 2, "BinaryReader::ReadOneChar assumes it's reading one or 2 bytes only.");

                try
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
                    charsRead = decoder.GetChars(new ReadOnlySpan<byte>(charBytes, 0, numBytes), singleChar,
                        flush: false);
#else
                    charsRead = decoder.GetChars(charBytes, 0, numBytes, singleChar, 0, false);
#endif
                }
                catch
                {
                    // Handle surrogate char

                    offset = posSav;
                    // else - we can't do much here

                    throw;
                }

                Debug.Assert(charsRead < 2, "BinaryReader::ReadOneChar - assuming we only got 0 or 1 char, not 2!");
            }

            Debug.Assert(charsRead > 0);
            return singleChar[0];
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
            this.buffer.Slice(offset, buffer.Length).CopyTo(buffer);
            return buffer.Length;
        }

        public int Read(Span<char> buffer)
        {
            int totalCharsRead = 0;
#if !NETSTANDARD2_1_OR_GREATER
            var byteBuffer = this.buffer.ToArray();
            var charBuffer = buffer.ToArray();
#endif
            while (!buffer.IsEmpty)
            {
                int numBytes = buffer.Length;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
                int charsRead = decoder.GetChars(this.buffer.Slice(offset), buffer, false);
                offset += encoding.GetByteCount(buffer.Slice(0, charsRead));
                buffer = buffer.Slice(charsRead);
#else
                int charsRead = decoder.GetChars(byteBuffer, offset, numBytes, charBuffer, totalCharsRead, false);
                offset += encoding.GetByteCount(charBuffer, 0, charsRead);
                buffer = buffer.Slice(charsRead);
#endif
                
                totalCharsRead += charsRead;
            }

            return totalCharsRead;
        }

        public int Read7BitEncodedInt()
        {
            // Unlike writing, we can't delegate to the 64-bit read on
            // 64-bit platforms. The reason for this is that we want to
            // stop consuming bytes if we encounter an integer overflow.

            uint result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 5 bytes,
            // or the fifth byte is about to cause integer overflow.
            // This means that we can read the first 4 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 4;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int)result; // early exit
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.

            byteReadJustNow = ReadByte();
            if (byteReadJustNow > 0b_1111u)
            {
                throw new FormatException();
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int)result;
        }

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

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public byte ReadByte()
        {
            return buffer[offset++];
        }

        public byte[] ReadBytes(int count)
        {
            var bf = new byte[count];
            Read(new Span<byte>(bf));
            return bf;
        }

        public char ReadChar()
        {
            int value = Read();
            if (value == -1)
            {
                ThrowHelper.ThrowIndexOutOfRange();
            }

            return (char)value;
        }

        public char[] ReadChars(int count)
        {
            var ch = new char[count];
            Read(new Span<char>(ch));
            return ch;
        }

        public decimal ReadDecimal()
        {
            const int SignMask = unchecked((int)0x80000000);
            int lo = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
            int mid = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset + 4));
            int hi = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset + 8));
            int flags = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset + 12));
            var scale = (byte)(flags >> 16);
            bool isNegative = (flags & SignMask) != 0;
            offset += 16;
            return new decimal(lo, mid, hi, isNegative, scale);
        }

        public double ReadDouble()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToDouble(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToDouble(slice, 0);
#endif
            offset += sizeof(double);
            return ret;
        }

        public short ReadInt16()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToInt16(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToInt16(slice, 0);
#endif
            offset += sizeof(short);
            return ret;
        }

        public int ReadInt32()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToInt32(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToInt32(slice, 0);
#endif
            offset += sizeof(int);
            return ret;
        }

        public long ReadInt64()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToInt64(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToInt64(slice, 0);
#endif
            offset += sizeof(long);
            return ret;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public float ReadSingle()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToSingle(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToSingle(slice, 0);
#endif
            offset += sizeof(float);
            return ret;
        }

        public string ReadString()
        {
            var byteSize = Read7BitEncodedInt();
            if (byteSize < 0)
                throw new IOException("BinaryReader encountered an invalid string length of -1 characters.");
            if (byteSize == 0)
                return string.Empty;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var slice = this.buffer.Slice(offset, byteSize);
#else
            var slice = new byte[buffer.Length - offset];
            buffer.Slice(offset).CopyTo(slice);
#endif
            return encoding.GetString(slice);
        }

        public ushort ReadUInt16()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToUInt16(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToUInt16(slice, 0);
#endif
            offset += sizeof(ushort);
            return ret;
        }

        public uint ReadUInt32()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToUInt32(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToUInt32(slice, 0);
#endif
            offset += sizeof(uint);
            return ret;
        }

        public ulong ReadUInt64()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            var ret = BitConverter.ToUInt64(buffer.Slice(offset));
#else
            var slice = new byte[sizeof(double)];
            buffer.Slice(offset, sizeof(double)).CopyTo(slice);
            var ret = BitConverter.ToUInt64(slice, 0);
#endif
            offset += sizeof(ulong);
            return ret;
        }

        public void ReadBytes(Span<byte> buffer)
        {
            this.buffer.Slice(offset, buffer.Length).CopyTo(buffer);
        }
    }
}