using NonSucking.Framework.Serialization;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using static DEMO.SUTMessage;

namespace DEMO
{

    public partial class ComplaingBaseBase
    {
        public string TestBase { get; set; }
    }

    [Nooson]

    public partial class ComplainBaseWithCtor : ComplaingBaseBase
    {
        //[NoosonCustom(SerializeMethodName = "FirstSerialize", SerializeImplementationType = typeof(ComplainBaseWithCtor))]
        //public User ComplainUser { get; set; }
        public string Complain { get; set; }
        [NoosonCustom(SerializeMethodName = "FirstSerialize", DeserializeMethodName = "FirstDeserialize")]
        public string FirstCustom { get; set; }
        [NoosonOrder(0)]
        public string Second { get; set; }
        [NoosonOrder(2)]
        public string Last { get; set; }
        public string Origin { get; }
        //public string Never { set => valueNever = value; }
        [NoosonOrder(5)]
        public string Ultimate { get; private set; }

        private readonly string valueNever;
        public ComplainBaseWithCtor(string complain, string valueNever)
        {
            Complain = complain;
            this.valueNever = valueNever;
        }

        public void FirstSerialize(BinaryWriter bw)
        {
            bw.Write(FirstCustom);
        }

        public static void FirstSerialize(BinaryWriter bw, string first)
        {
            bw.Write(first);
        }

        public static string FirstDeserialize(BinaryReader br)
        {
            return br.ReadString();
        }


        [NoosonPreferredCtor]
        public ComplainBaseWithCtor([NoosonParameter(nameof(Ultimate))] string first, string second, string last)
        {
            Complain = first + second + last;
        }
        public ComplainBaseWithCtor()
        {
        }
    }

    [Nooson]
    public partial class SUTMessage
    {
        public Point[] Positions { get; set; }
        [NoosonIgnore]
        public int Type { get; set; }
        public string Text { get; set; }
        public List<User> UsersList { get; set; }
        public List<ComplainBase> ComplainsBases { get; set; }
        public List<short> Countings { get; set; }
        public Dictionary<short, ComplainBase> CountingDic { get; set; }
        public IReadOnlyList<short> ReadOnlyCountings { get; }
        public IReadOnlyList<short> ReadOnlyCountingsButSetable { get; set; }
        //public IEnumerable<short> ThisIsAListAsIEnumerable { get; }
        //public IEnumerable ThisIsNotSupportedIEnumerable { get; }
        public AccessRight Right { get; set; }
        public ComplainBase Complain { get; set; }
        public User AssignedUser { get; set; }
        public Point Position { get; set; }
        public IUser ContactUser { get; }
        //[NoosonCustom(SerializeMethodName =nameof(SerializeIUser), SerializeImplementationType = typeof(SUTMessage), DeserializeMethodName =nameof(DeserializeIUser))]
        public IUser AlternativUser { get; set; }
        public int X { get; set; }

        public int countPositions { get; set; }

        public enum AccessRight
        {
            A, B, C
        }

        public SUTMessage()
        {
            ReadOnlyCountings = new List<short>();
            ContactUser = new User() { Name = " " };
        }

        public static void SerializeIUser(BinaryWriter bw, IUser user)
        {
            bw.Write(user.Name);
        }

        public static IUser DeserializeIUser(BinaryReader br)
        {
            return new User() { Name = br.ReadString() };
        }


        [NoosonCustom(SerializeMethodName = "SerializeMe", DeserializeMethodName = "DeserializeMe")]

        public class User : IUser
        {
            public string Name { get; set; }

            public int DoSomething()
                => 12;

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Name);
            }

            public void SerializeMe(BinaryWriter bw)
            {
                bw.Write(Name);
            }

            public static void SerializeMe(BinaryWriter bw, IUser user)
            {
                bw.Write(user.Name);
            }
            public static User Deserialize(BinaryReader reader)
            {
                return new User() { Name = reader.ReadString() };
            }
            public static User DeserializeMe(BinaryReader reader)
            {
                return new User() { Name = reader.ReadString() };
            }
        }
    }



    //public partial class SUTMessage
    //{
    //    public void Serialize(BinaryWriter writer)
    //    {
    //        writer.Write((int)this.Right);
    //        this.Complain.Serialize(writer);
    //        foreach (var item in this.ThisIsAListAsIEnumerable)
    //        {
    //            writer.Write(item);
    //        }
    //        writer.Write(this.Type);
    //        writer.Write(this.ReadOnlyCountings.Count);
    //        foreach (var item in this.ReadOnlyCountings)
    //        {
    //            writer.Write(item);
    //        }
    //        writer.Write(this.Countings.Capacity);
    //        writer.Write(this.Countings.Count);
    //        foreach (var item in this.Countings)
    //        {
    //            writer.Write(item);
    //        }
    //        writer.Write(this.Users.Capacity);
    //        writer.Write(this.Users.Count);
    //        foreach (var item in this.Users)
    //        {
    //            writer.Write(item.Name);
    //        }
    //        writer.Write(this.Position.IsEmpty);
    //        writer.Write(this.Position.X);
    //        writer.Write(this.Position.Y);
    //        foreach (var item in this.Positions)
    //        {
    //            writer.Write(item.IsEmpty);
    //            writer.Write(item.X);
    //            writer.Write(item.Y);
    //        }
    //        writer.Write(this.Text);
    //        this.AssignedUser.Serialize(writer);
    //    }

    //}


    //[Nooson]
    public partial class ComplainBase
    {
        public string Complain { get; set; }
        public List<string> Complains { get; set; }

        //public ComplainBase(string abc)
        //{

        //}
    }
}
