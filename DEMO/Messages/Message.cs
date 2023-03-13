
using System.Text;

using NonSucking.Framework.Serialization;

namespace BotMaster.PluginSystem.Messages
{
    [Nooson]
    public partial class Message
    {
        public static Message Empty = new Message(Guid.Empty, MessageType.None, Array.Empty<byte>());

        public const int HeaderSize = sizeof(MessageType) + sizeof(int) + sizeof(int);
        [NoosonIgnore]
        public static Encoding Encoding { get; } = Encoding.UTF8;
        [NoosonIgnore]
        public static int NextId { get => nextId++; }
        private static int nextId = 0;


        public DateTime MyProperty { get; set; }
        private int id = -1;

        public int Id
        {
            get {
                if (id == -1)
                    id = NextId;
                return id;
            } set => id = value;
        }

        public MessageType Type { get; }

        /// <summary>
        /// Taget null or Empty = Broadcast
        /// 'Server id' => Server listens as Plugin
        ///
        /// </summary>
        public string TargetId { get; }

        public Guid ContractUID { get; }

        [NoosonIgnore]
        public IReadOnlyList<byte> Data => data;


        [NoosonInclude]
        private readonly byte[] data;

        public Message(Guid contractUID, MessageType type, byte[] data, string targetId = null)
        {
            ContractUID = contractUID;
            Type = type;
            TargetId = targetId;
            this.data = data;
        }

        public ReadOnlySpan<byte> DataAsSpan()
            => data;

    }
}
