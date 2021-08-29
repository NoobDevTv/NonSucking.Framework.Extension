using NonSucking.Framework.Extension.Serialization;
using System;
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
    [Nooson]
    public partial class ComplainBaseWithCtor
    {
        public string Complain { get; set; }
        public string First { get; set; }
        public string Second { get; set; }
        public string Last { get; set; }

        public ComplainBaseWithCtor(string complain)
        {
            Complain = complain;
        }


        public ComplainBaseWithCtor(string first, string second, string last)
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
        [NoosonIgnore]
        public int Type { get; set; }
        public string Text { get; set; }
        public List<User> Users { get; set; }
        public List<short> Countings { get; set; }
        public IReadOnlyList<short> ReadOnlyCountings { get; }
        public IEnumerable<short> ThisIsAListAsIEnumerable { get; }
        public AccessRight Right { get; set; }
        public ComplainBase Complain { get; set; }
        public User AssignedUser { get; set; }
        public Point Position { get; set; }
        public Point[] Positions { get; set; }
        public IUser ContactUser { get; }
        public IUser AlternativUser { get; set; }
        public int X { get; set; }

        public enum AccessRight
        {
            A, B, C
        }

        public SUTMessage()
        {
        }

      

        public class User : IUser
        {
            public string Name { get; set; }

            public int DoSomething()
                => 12;

            public void Serialize(BinaryWriter writer)
            {

            }

            public static User Deserialize(BinaryReader reader)
            {
                return new User();
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


    [Nooson]
    public partial class ComplainBase
    {
        public string Complain { get; set; }
    }
}
