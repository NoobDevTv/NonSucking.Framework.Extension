namespace DEMO
{
    public partial class SinglePropTest
    {
        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Position2.X);
            writer.Write(Position2.Y);
            writer.Write(IsEmpty);
            writer.Write(Test2);
        }

        public static SinglePropTest Deserialize(System.IO.BinaryReader reader)
        {
            System.Drawing.Point Position__ret__e;
            var X__Position__c = reader.ReadInt32();
            var Y__Position__d = reader.ReadInt32();
            Position__ret__e = new System.Drawing.Point(X__Position__c, Y__Position__d);
            System.Drawing.Point Position2__ret__i;
            var X__Position2__g = reader.ReadInt32();
            var Y__Position2__h = reader.ReadInt32();
            Position2__ret__i = new System.Drawing.Point(X__Position2__g, Y__Position2__h);
            var IsEmpty__ret__j = reader.ReadBoolean();
            var Test2__ret__k = reader.ReadInt32();
            var ret____ = new DEMO.SinglePropTest();
            ret____.IsEmpty = IsEmpty__ret__j;
            ret____.Position2 = Position2__ret__i;
            ret____.Position = Position__ret__e;
            ret____.Test2 = Test2__ret__k;
            return ret____;
        }
    }
}