using System;
using System.IO;

namespace NonSucking.Framework.Serialization
{
    /// <summary>
    /// Extension methods for <see cref="BinaryReader"/>.
    /// </summary>
    public static class BinaryReaderExtensions
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
    }
}
