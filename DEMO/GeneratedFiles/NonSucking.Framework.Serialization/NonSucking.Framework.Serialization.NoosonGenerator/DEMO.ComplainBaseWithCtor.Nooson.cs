namespace DEMO
{
    public partial class ComplainBaseWithCtor
    {
        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Second);
            writer.Write(Last);
            writer.Write(Ultimate);
            FirstSerialize(writer);
            writer.Write(Origin);
            writer.Write(valueNever);
            DEMO.ComplainBaseWithCtor.FirstSerialize(writer, serializeThisFieldForMe);
        }

        public static ComplainBaseWithCtor Deserialize(System.IO.BinaryReader reader)
        {
            var Second__ret__l = reader.ReadString();
            var Last__ret__m = reader.ReadString();
            var Ultimate__ret__n = reader.ReadString();
            var FirstCustom__ret__o = FirstDeserialize(reader);
            var Origin__ret__p = reader.ReadString();
            var valueNever__ret__q = reader.ReadString();
            var serializeThisFieldForMe__ret__r = FirstDeserialize(reader);
            var ret____ = new DEMO.ComplainBaseWithCtor(Ultimate__ret__n, Second__ret__l, Last__ret__m);
            ret____.FirstCustom = FirstCustom__ret__o;
            ret____.serializeThisFieldForMe = serializeThisFieldForMe__ret__r;
            return ret____;
        }
    }
}