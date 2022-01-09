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

    [Nooson]
    public partial class ComplaingBaseBase
    {
        [NoosonIgnore]
        public string InitOnlyABC { get; init; }

    }

    [Nooson]
    public partial class ComplainBaseWithCtor : ComplaingBaseBase
    {
        //[NoosonCustom(SerializeMethodName = "FirstSerialize", SerializeImplementationType = typeof(ComplainBaseWithCtor))]
        //public User ComplainUser { get; set; }
        [NoosonIgnore]
        public string Complain { get; init; }
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

        [NoosonInclude]
        private readonly string valueNever;

        [NoosonInclude]
        [NoosonCustom(SerializeMethodName = "FirstSerialize", SerializeImplementationType = typeof(ComplainBaseWithCtor), DeserializeMethodName = "FirstDeserialize")]
        private string serializeThisFieldForMe = "";
        [NoosonPreferredCtor]
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


        public ComplainBaseWithCtor([NoosonParameter(nameof(Ultimate))] string first, string second, string last)
        {
            Complain = first + second + last;
        }
        public ComplainBaseWithCtor()
        {
        }
    }

    [Nooson]
    public partial class SUTMessage : IEquatable<SUTMessage>
    {
        public Point[] Positions { get; set; }
        //[NoosonIgnore]
        public int Type { get; set; }
        public string Text { get; set; }
        public List<User> UsersList { get; set; }
        public List<ComplainBase> ComplainsBases { get; set; }
        public List<short> Countings { get; set; }
        public Dictionary<short, ComplainBase> CountingDic { get; set; }
        public IReadOnlyList<short> ReadOnlyCountings { get; }
        public IReadOnlyList<short> ReadOnlyCountingsButSetable { get; set; }
        public IReadOnlyDictionary<short, short> ReadOnlyDicSetable { get; set; }
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

        [NoosonInclude]
        private int randomField = new Random().Next();

        [NoosonInclude]
        private readonly string forCtor = DateTime.Now.ToString("HH:mm:ss.fffffff");

        public enum AccessRight
        {
            A, B, C
        }

        public SUTMessage()
        {
            ReadOnlyCountings = new List<short>();
            ContactUser = new User() { Name = " " };
        }

        [NoosonPreferredCtor]
        public SUTMessage([NoosonParameter(nameof(forCtor))] string createTime) : this()
        {
            forCtor = createTime;
        }

        public static void SerializeIUser(BinaryWriter bw, IUser user)
        {
            bw.Write(user.Name);
        }

        public static IUser DeserializeIUser(BinaryReader br)
        {
            return new User() { Name = br.ReadString() };
        }

        public override bool Equals(object obj) => Equals(obj as SUTMessage);
        public bool Equals(SUTMessage other) =>
            other is not null
            && Positions.SequenceEqual(other.Positions)
            && Type == other.Type
            && Text == other.Text
            && UsersList.SequenceEqual(other.UsersList)
            && ComplainsBases.SequenceEqual(other.ComplainsBases)
            && Countings.SequenceEqual(other.Countings)
            && CountingDic.SequenceEqual(other.CountingDic)
            && ReadOnlyCountings.SequenceEqual(other.ReadOnlyCountings)
            && ReadOnlyCountingsButSetable.SequenceEqual(other.ReadOnlyCountingsButSetable)
            && ReadOnlyDicSetable.SequenceEqual(other.ReadOnlyDicSetable)
            && Right == other.Right
            && EqualityComparer<ComplainBase>.Default.Equals(Complain, other.Complain)
            && EqualityComparer<User>.Default.Equals(AssignedUser, other.AssignedUser)
            && Position.Equals(other.Position)
            && EqualityComparer<IUser>.Default.Equals(ContactUser, other.ContactUser)
            && EqualityComparer<IUser>.Default.Equals(AlternativUser, other.AlternativUser)
            && X == other.X
            && countPositions == other.countPositions
            && randomField == other.randomField
            && forCtor == other.forCtor;
        /*
         EqualityComparer<List<User>>.Default.Equals(UsersList, other.UsersList) && EqualityComparer<List<ComplainBase>>.Default.Equals(ComplainsBases, other.ComplainsBases) && EqualityComparer<List<short>>.Default.Equals(Countings, other.Countings) && EqualityComparer<Dictionary<short, ComplainBase>>.Default.Equals(CountingDic, other.CountingDic) && EqualityComparer<IReadOnlyList<short>>.Default.Equals(ReadOnlyCountings, other.ReadOnlyCountings) && EqualityComparer<IReadOnlyList<short>>.Default.Equals(ReadOnlyCountingsButSetable, other.ReadOnlyCountingsButSetable)         
        */
        public static bool operator ==(SUTMessage left, SUTMessage right) => EqualityComparer<SUTMessage>.Default.Equals(left, right);
        public static bool operator !=(SUTMessage left, SUTMessage right) => !(left == right);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Positions);
            hash.Add(Type);
            hash.Add(Text);
            hash.Add(UsersList);
            hash.Add(ComplainsBases);
            hash.Add(Countings);
            hash.Add(CountingDic);
            hash.Add(ReadOnlyCountings);
            hash.Add(ReadOnlyCountingsButSetable);
            hash.Add(Right);
            hash.Add(Complain);
            hash.Add(AssignedUser);
            hash.Add(Position);
            hash.Add(ContactUser);
            hash.Add(AlternativUser);
            hash.Add(X);
            hash.Add(countPositions);
            hash.Add(randomField);
            hash.Add(forCtor);
            return hash.ToHashCode();
        }

        [NoosonCustom(SerializeMethodName = "SerializeMe", DeserializeMethodName = "DeserializeMe")]

        public class User : IUser, IEquatable<User>
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

            public override bool Equals(object obj) => Equals(obj as User);
            public bool Equals(User other) => other != null && Name == other.Name;
            public override int GetHashCode() => HashCode.Combine(Name);

            public static bool operator ==(User left, User right) => EqualityComparer<User>.Default.Equals(left, right);
            public static bool operator !=(User left, User right) => !(left == right);
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
    public partial class ComplainBase : IEquatable<ComplainBase>
    {
        public string Complain { get; set; }
        public List<string> Complains { get; set; }

        public override bool Equals(object obj) => Equals(obj as ComplainBase);
        public bool Equals(ComplainBase other) => other != null && Complain == other.Complain && Complains.SequenceEqual(other.Complains);
        public override int GetHashCode() => HashCode.Combine(Complain, Complains);

        public static bool operator ==(ComplainBase left, ComplainBase right) => EqualityComparer<ComplainBase>.Default.Equals(left, right);
        public static bool operator !=(ComplainBase left, ComplainBase right) => !(left == right);

        //public ComplainBase(string abc)
        //{

        //}
    }
}
