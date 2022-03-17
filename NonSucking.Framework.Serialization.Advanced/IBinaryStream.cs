using System;
using System.IO;
using System.Xml;

namespace NonSucking.Framework.Serialization
{
    public interface IBinaryReader
    {
                /// <summary>Returns the next available character and does not advance the byte or character position.</summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.ArgumentException">The current character cannot be decoded into the internal character buffer by using the <see cref="T:System.Text.Encoding" /> selected for the stream.</exception>
        /// <returns>The next available character, or -1 if no more characters are available or the stream does not support seeking.</returns>
        int PeekChar();

        /// <summary>Reads characters from the underlying stream and advances the current position of the stream in accordance with the <see langword="Encoding" /> used and the specific character being read from the stream.</summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <returns>The next character from the input stream, or -1 if no characters are currently available.</returns>
        int Read();

        /// <summary>Reads the specified number of bytes from the stream, starting from a specified point in the byte array.</summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />.
        /// 
        /// -or-
        /// 
        /// The number of decoded characters to read is greater than <paramref name="count" />. This can happen if a Unicode decoder returns fallback characters or a surrogate pair.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>The number of bytes read into <paramref name="buffer" />. This might be less than the number of bytes requested if that many bytes are not available, or it might be zero if the end of the stream is reached.</returns>
        int Read(byte[] buffer, int index, int count);

        /// <summary>Reads the specified number of characters from the stream, starting from a specified point in the character array.</summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
        /// <param name="count">The number of characters to read.</param>
        /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />.
        /// 
        /// -or-
        /// 
        /// The number of decoded characters to read is greater than <paramref name="count" />. This can happen if a Unicode decoder returns fallback characters or a surrogate pair.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>The total number of characters read into the buffer. This might be less than the number of characters requested if that many characters are not currently available, or it might be zero if the end of the stream is reached.</returns>
        int Read(char[] buffer, int index, int count);

        /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current source.</param>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        int Read(Span<byte> buffer);

        /// <summary>Reads, from the current stream, the same number of characters as the length of the provided buffer, writes them in the provided buffer, and advances the current position in accordance with the <see langword="Encoding" /> used and the specific character being read from the stream.</summary>
        /// <param name="buffer">A span of characters. When this method returns, the contents of this region are replaced by the characters read from the current source.</param>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>The total number of characters read into the buffer. This might be less than the number of characters requested if that many characters are not currently available, or it might be zero if the end of the stream is reached.</returns>
        int Read(Span<char> buffer);

        /// <summary>Reads in a 32-bit integer in compressed format.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.FormatException">The stream is corrupted.</exception>
        /// <returns>A 32-bit integer in compressed format.</returns>
        int Read7BitEncodedInt();

        /// <summary>Reads a number 7 bits at a time.</summary>
        /// <returns>The number that is read from this binary reader instance.</returns>
        long Read7BitEncodedInt64();

        /// <summary>Reads a <see langword="Boolean" /> value from the current stream and advances the current position of the stream by one byte.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>
        /// <see langword="true" /> if the byte is nonzero; otherwise, <see langword="false" />.</returns>
        bool ReadBoolean();

        /// <summary>Reads the next byte from the current stream and advances the current position of the stream by one byte.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>The next byte read from the current stream.</returns>
        byte ReadByte();

        /// <summary>Reads the specified number of bytes from the current stream into a byte array and advances the current position by that number of bytes.</summary>
        /// <param name="count">The number of bytes to read. This value must be 0 or a non-negative number or an exception will occur.</param>
        /// <exception cref="T:System.ArgumentException">The number of decoded characters to read is greater than <paramref name="count" />. This can happen if a Unicode decoder returns fallback characters or a surrogate pair.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="count" /> is negative.</exception>
        /// <returns>A byte array containing data read from the underlying stream. This might be less than the number of bytes requested if the end of the stream is reached.</returns>
        byte[] ReadBytes(int count);

        /// <summary>Reads the next character from the current stream and advances the current position of the stream in accordance with the <see langword="Encoding" /> used and the specific character being read from the stream.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.ArgumentException">A surrogate character was read.</exception>
        /// <returns>A character read from the current stream.</returns>
        char ReadChar();

        /// <summary>Reads the specified number of characters from the current stream, returns the data in a character array, and advances the current position in accordance with the <see langword="Encoding" /> used and the specific character being read from the stream.</summary>
        /// <param name="count">The number of characters to read.</param>
        /// <exception cref="T:System.ArgumentException">The number of decoded characters to read is greater than <paramref name="count" />. This can happen if a Unicode decoder returns fallback characters or a surrogate pair.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="count" /> is negative.</exception>
        /// <returns>A character array containing data read from the underlying stream. This might be less than the number of characters requested if the end of the stream is reached.</returns>
        char[] ReadChars(int count);

        /// <summary>Reads a decimal value from the current stream and advances the current position of the stream by sixteen bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A decimal value read from the current stream.</returns>
        Decimal ReadDecimal();

        /// <summary>Reads an 8-byte floating point value from the current stream and advances the current position of the stream by eight bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>An 8-byte floating point value read from the current stream.</returns>
        double ReadDouble();

        /// <summary>Reads a 2-byte signed integer from the current stream and advances the current position of the stream by two bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A 2-byte signed integer read from the current stream.</returns>
        short ReadInt16();

        /// <summary>Reads a 4-byte signed integer from the current stream and advances the current position of the stream by four bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A 4-byte signed integer read from the current stream.</returns>
        int ReadInt32();

        /// <summary>Reads an 8-byte signed integer from the current stream and advances the current position of the stream by eight bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>An 8-byte signed integer read from the current stream.</returns>
        long ReadInt64();

        /// <summary>Reads a signed byte from this stream and advances the current position of the stream by one byte.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A signed byte read from the current stream.</returns>
        [CLSCompliant(false)]
        sbyte ReadSByte();

        /// <summary>Reads a 4-byte floating point value from the current stream and advances the current position of the stream by four bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A 4-byte floating point value read from the current stream.</returns>
        float ReadSingle();

        /// <summary>Reads a string from the current stream. The string is prefixed with the length, encoded as an integer seven bits at a time.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>The string being read.</returns>
        string ReadString();

        /// <summary>Reads a 2-byte unsigned integer from the current stream using little-endian encoding and advances the position of the stream by two bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A 2-byte unsigned integer read from this stream.</returns>
        [CLSCompliant(false)]
        ushort ReadUInt16();

        /// <summary>Reads a 4-byte unsigned integer from the current stream and advances the position of the stream by four bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <returns>A 4-byte unsigned integer read from this stream.</returns>
        [CLSCompliant(false)]
        uint ReadUInt32();

        /// <summary>Reads an 8-byte unsigned integer from the current stream and advances the position of the stream by eight bytes.</summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <returns>An 8-byte unsigned integer read from this stream.</returns>
        [CLSCompliant(false)]
        ulong ReadUInt64();

        void ReadBytes(Span<byte> buffer);
    }
    public interface IBinaryWriter
    {
        /// <summary>Writes a one-byte <see langword="Boolean" /> value to the current stream, with 0 representing <see langword="false" /> and 1 representing <see langword="true" />.</summary>
        /// <param name="value">The <see langword="Boolean" /> value to write (0 or 1).</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(bool value);

        /// <summary>Writes an unsigned byte to the current stream and advances the stream position by one byte.</summary>
        /// <param name="value">The unsigned byte to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(byte value);

        /// <summary>Writes a byte array to the underlying stream.</summary>
        /// <param name="buffer">A byte array containing the data to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        void Write(byte[] buffer);

        /// <summary>Writes a region of a byte array to the current stream.</summary>
        /// <param name="buffer">A byte array containing the data to write.</param>
        /// <param name="index">The index of the first byte to read from <paramref name="buffer" /> and to write to the stream.</param>
        /// <param name="count">The number of bytes to read from <paramref name="buffer" /> and to write to the stream.</param>
        /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(byte[] buffer, int index, int count);

        /// <summary>Writes a Unicode character to the current stream and advances the current position of the stream in accordance with the <see langword="Encoding" /> used and the specific characters being written to the stream.</summary>
        /// <param name="ch">The non-surrogate, Unicode character to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="ch" /> is a single surrogate character.</exception>
        void Write(char ch);

        /// <summary>Writes a character array to the current stream and advances the current position of the stream in accordance with the <see langword="Encoding" /> used and the specific characters being written to the stream.</summary>
        /// <param name="chars">A character array containing the data to write.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="chars" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        void Write(char[] chars);

        /// <summary>Writes a section of a character array to the current stream, and advances the current position of the stream in accordance with the <see langword="Encoding" /> used and perhaps the specific characters being written to the stream.</summary>
        /// <param name="chars">A character array containing the data to write.</param>
        /// <param name="index">The index of the first character to read from <paramref name="chars" /> and to write to the stream.</param>
        /// <param name="count">The number of characters to read from <paramref name="chars" /> and to write to the stream.</param>
        /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="chars" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(char[] chars, int index, int count);

        /// <summary>Writes a decimal value to the current stream and advances the stream position by sixteen bytes.</summary>
        /// <param name="value">The decimal value to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(Decimal value);

        /// <summary>Writes an eight-byte floating-point value to the current stream and advances the stream position by eight bytes.</summary>
        /// <param name="value">The eight-byte floating-point value to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(double value);

        /// <summary>Writes a two-byte signed integer to the current stream and advances the stream position by two bytes.</summary>
        /// <param name="value">The two-byte signed integer to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(short value);

        /// <summary>Writes a four-byte signed integer to the current stream and advances the stream position by four bytes.</summary>
        /// <param name="value">The four-byte signed integer to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(int value);

        /// <summary>Writes an eight-byte signed integer to the current stream and advances the stream position by eight bytes.</summary>
        /// <param name="value">The eight-byte signed integer to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(long value);

        /// <summary>Writes a span of bytes to the current stream.</summary>
        /// <param name="buffer">The span of bytes to write.</param>
        void Write(ReadOnlySpan<byte> buffer);

        /// <summary>Writes a span of characters to the current stream, and advances the current position of the stream in accordance with the <see langword="Encoding" /> used and perhaps the specific characters being written to the stream.</summary>
        /// <param name="chars">A span of chars to write.</param>
        void Write(ReadOnlySpan<char> chars);

        /// <summary>Writes a signed byte to the current stream and advances the stream position by one byte.</summary>
        /// <param name="value">The signed byte to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        [CLSCompliant(false)]
        void Write(sbyte value);

        /// <summary>Writes a four-byte floating-point value to the current stream and advances the stream position by four bytes.</summary>
        /// <param name="value">The four-byte floating-point value to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(float value);

        /// <summary>Writes a length-prefixed string to this stream in the current encoding of the <see cref="T:System.IO.BinaryWriter" />, and advances the current position of the stream in accordance with the encoding used and the specific characters being written to the stream.</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        void Write(string value);

        /// <summary>Writes a two-byte unsigned integer to the current stream and advances the stream position by two bytes.</summary>
        /// <param name="value">The two-byte unsigned integer to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        [CLSCompliant(false)]
        void Write(ushort value);

        /// <summary>Writes a four-byte unsigned integer to the current stream and advances the stream position by four bytes.</summary>
        /// <param name="value">The four-byte unsigned integer to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        [CLSCompliant(false)]
        void Write(uint value);

        /// <summary>Writes an eight-byte unsigned integer to the current stream and advances the stream position by eight bytes.</summary>
        /// <param name="value">The eight-byte unsigned integer to write.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        [CLSCompliant(false)]
        void Write(ulong value);

        /// <summary>Writes a 32-bit integer in a compressed format.</summary>
        /// <param name="value">The 32-bit integer to be written.</param>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.IO.IOException">The stream is closed.</exception>
        void Write7BitEncodedInt(int value);

        /// <summary>Writes out a number 7 bits at a time.</summary>
        /// <param name="value">The value to write.</param>
        void Write7BitEncodedInt64(long value);
    }

    public interface IBinaryStream : IBinaryReader, IBinaryWriter
    {
        
    }
}
