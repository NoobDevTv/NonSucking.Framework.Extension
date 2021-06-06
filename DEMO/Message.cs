using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEMO
{
    public partial class Message
    {
        public int Type { get; set; }
        public string Text { get; set; }
        public List<short> Countings { get; set; }
        public IReadOnlyList<short> ReadOnlyCountings { get; }
        public AccessRight Right { get; set; }
        public ComplainBase Complain { get; set; }
        public Point Position { get; set; }

        public enum AccessRight
        {
            A, B, C
        }

        public partial class ComplainBase
        {
            public int Size() => 0;
            public void Serialize(BinaryWriter writer) { }
            public int Serialize(Span<byte> span) => 0;
        }
    }

    public partial class Message
    {
        public int Size()
        {
            return
            sizeof(int)
                + (sizeof(int) + Encoding.UTF8.GetByteCount(Text))
                + (Countings.Count * sizeof(short))
                + (ReadOnlyCountings.Count * sizeof(short))
                + sizeof(int)
                + Complain.Size()
                + SizeOf(Position);
        }

        private static int SizeOf(Point color)
        {
            return
                sizeof(int)
                + sizeof(int)
                + sizeof(bool);
        }

        public int Serialize(Span<byte> span)
        {
            int index = 0;
            BitConverter.TryWriteBytes(span, Type);
            index += sizeof(int);

            var textSpan = Text.AsSpan();
            BitConverter.TryWriteBytes(span[index..], textSpan.Length);
            index += sizeof(int);
            index += Encoding.UTF8.GetBytes(textSpan, span);

            BitConverter.TryWriteBytes(span[index..], Countings.Count);
            index += sizeof(int);
            foreach (var item in Countings)
            {
                BitConverter.TryWriteBytes(span[index..], item);
                index += sizeof(short);
            }

            BitConverter.TryWriteBytes(span[index..], ReadOnlyCountings.Count);
            index += sizeof(int);
            foreach (var item in ReadOnlyCountings)
            {
                BitConverter.TryWriteBytes(span[index..], item);
                index += sizeof(short);
            }

            BitConverter.TryWriteBytes(span[index..], (int)Right);
            index += sizeof(int);

            index += Complain.Serialize(span);

            BitConverter.TryWriteBytes(span[index..], Position.IsEmpty);
            index += sizeof(bool);
            BitConverter.TryWriteBytes(span[index..], Position.X);
            index += sizeof(int);
            BitConverter.TryWriteBytes(span[index..], Position.Y);
            return index += sizeof(int);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Text);
            writer.Write(Countings.Count);
            foreach (var item in Countings)
            {
                writer.Write(item);
            }
            writer.Write(ReadOnlyCountings.Count);
            foreach (var item in ReadOnlyCountings)
            {
                writer.Write(item);
            }
            writer.Write((int)Right);
            Complain.Serialize(writer);
            writer.Write(Position.IsEmpty);
            writer.Write(Position.X);
            writer.Write(Position.Y);
        }


        //public static Message Deserialize(Span<byte> span)
        //{

        //}
        //public static Message Deserialize(BinaryReader reader)
        //{

        //}
    }
}
