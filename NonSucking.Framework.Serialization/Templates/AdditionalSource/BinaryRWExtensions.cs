using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NonSucking.Framework.Serialization
{
    /// <summary>
    /// Extension methods for <see cref="BinaryReader"/> and <see cref="BinaryWriter"/>.
    /// </summary>
    public static class BinaryRWExtensions
    {
        /// <summary>
        /// Read exact number of bytes from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The reader to read bytes from.</param>
        /// <param name="buffer">The buffer to read bytes into.</param>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the <paramref name="reader"/> base stream is finished before completing the read.
        /// </exception>
        public static void ReadBytes(this BinaryReader reader, Span<byte> buffer)
        {
            int read = 0;
            do
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
                var res = reader.Read(buffer.Slice(read));
#else
                var tmpBuffer = new byte[buffer.Length - read];
                var res = reader.Read(tmpBuffer, 0, tmpBuffer.Length);
                new Span<byte>(tmpBuffer, 0, read).CopyTo(buffer.Slice(read));
#endif
                if (res == -1)
                    throw new EndOfStreamException();
                read += res;
            } while (read < buffer.Length);
        }

        /// <summary>
        /// Write bytes to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The writer to write bytes into.</param>
        /// <param name="buffer">The buffer to write.</param>
        public static void WriteBytes(this BinaryWriter writer, Span<byte> buffer)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            writer.Write(buffer);
#else
            var tmpBuffer = new byte[buffer.Length];
            buffer.CopyTo(tmpBuffer);
            writer.Write(tmpBuffer, 0, tmpBuffer.Length);
#endif
        }

        /// <summary>
        /// Read an unmanaged type <typeparamref name="T"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <typeparam name="T">The unmanaged type to read.</typeparam>
        /// <returns>The read unmanaged type.</returns>
        public static T ReadUnmanaged<T>(this BinaryReader reader)
            where T : unmanaged
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
            reader.ReadBytes(buffer);
            return MemoryMarshal.Read<T>(buffer);
        }

        /// <summary>
        /// Writes an unmanaged type <typeparamref name="T"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The unmanaged value to write.</param>
        /// <typeparam name="T">The unmanaged type to write.</typeparam>
        public static void WriteUnmanaged<T>(this BinaryWriter writer, T value)
            where T : unmanaged
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
            MemoryMarshal.Write(buffer, ref value);
            writer.WriteBytes(buffer);
        }
    }
}
