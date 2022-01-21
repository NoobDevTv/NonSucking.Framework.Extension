using System;
using System.IO;

namespace NonSucking.Framework.Serialization
{
    public static class BinaryReaderExtensions
    {
        public static void ReadBytes(this BinaryReader reader, Span<byte> buffer)
        {
            int read = 0;
            do
            {
                var res = reader.Read(buffer.Slice(read));
                if (res == -1)
                    throw new EndOfStreamException();
                read += res;
            } while (read < buffer.Length);
        }
    }
}
