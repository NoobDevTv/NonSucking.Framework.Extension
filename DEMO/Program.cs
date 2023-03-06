
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using static DEMO.SUTMessage;

namespace DEMO
{
    class Program
    {
        static void Main(string[] args)
        {
            var dic = new Dictionary<int, int>();

            var dicCast = (ICollection<KeyValuePair<int, int>>)dic;
            dicCast.Add(new KeyValuePair<int, int>(11, 10));

            //var dic2 = (Dictionary<int, int>)dicCast;
            //var arrCast = (IList<int>)arr;
            //arrCast[0]= 12;


            var bll = new ByteLengthList() { new byte[] { 12, 13, 14 }, new byte[] { 45, 46, 47 } };
            bll.NameOfList = "ABC";

            var conv = Newtonsoft.Json.JsonConvert.SerializeObject(bll, new JsonSerializerSettings { });
            var sutMessage = new SUTMessage()
            {
                AlternativUser = new User { Name = "Okay" },
                AssignedUser = new User { Name = "OkayTest" },
                Complain = new ComplainBase { Complain = "SingleComplain", Complains = new List<string> { "MultiComplains", "MultiComplains2", "MultiComplains3" } },
                ComplainsBases = new()
                {
                    new ComplainBase { Complain = "SingleComplainInList", Complains = new List<string> { "MultiComplainsInList", "MultiComplainsInList2", "MultiComplainsInList3" } },
                    new ComplainBase { Complain = "SingleComplainInList1", Complains = new List<string> { "MultiComplainsInList1", "MultiComplainsInList12", "MultiComplainsInList13" } },
                    new ComplainBase { Complain = "SingleComplainInList2", Complains = new List<string> { "MultiComplainsInList2", "MultiComplainsInList22", "MultiComplainsInList23" } }
                },
                //ContactUser = new User { Name ="ContactUserName"},
                CountingDic = new()
                {
                    { 1, new ComplainBase() { Complain = "SingleComplainInDic", Complains = new List<string> { "MultiComplainsInDic", "MultiComplainsInDic2", "MultiComplainsInDic3" } } },
                    { 2, new ComplainBase() { Complain = "SingleComplainInDic", Complains = new List<string> { "MultiComplainsInDic2", "MultiComplainsInDic22", "MultiComplainsInDic23" } } },
                    { 3, new ComplainBase() { Complain = "SingleComplainInDic", Complains = new List<string> { "MultiComplainsInDic3", "MultiComplainsInDic32", "MultiComplainsInDic33" } } },
                    { 4, new ComplainBase() { Complain = "SingleComplainInDic", Complains = new List<string> { "MultiComplainsInDic4", "MultiComplainsInDic42", "MultiComplainsInDic43" } } }
                },
                Countings = new() { 1, 3, 4, 8, 9, 4564, 231, 8976, 687 },
                countPositions = 123121,
                Position = new Point(12, 99),
                Positions = new[] { new Point(777, 888), new Point(7770, 4594), new Point(445678, 42), new Point(6456, 4567), },
                ReadOnlyCountingsButSetable = new List<short> { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 2200 },
                ReadOnlyDicSetable = new Dictionary<short, short>() { { 22, 33 }, { 44, 55 }, { 6666, 7777 }, { 661, 789 }, },
                Right = AccessRight.A,
                Text = "Just a random text",
                Type = 897987414,
                UsersList = new List<User>() { new User() { Name = "1User" }, new User() { Name = "2User" }, new User() { Name = "3User" }, new User() { Name = "4User" } },
                X = 123123,
                UnmanagedTypes = new() { SomePos = new Point(1, 2), SomeTime = DateTime.Parse("2022-02-22 12:34")}
            };

            //var sut = new SinglePropTest
            //{
            //    IsEmpty = true,
            //    Position = new Point(12, 3),
            //    Position2 = new Point(45, 9)
            //};
            using (var ms = new FileStream("sut.save", FileMode.OpenOrCreate))
            {
                using var bw = new BinaryWriter(ms);
                sutMessage.Serialize(bw);
            }

            using (var ms = new FileStream("sut.save", FileMode.Open))
            {
                using var br = new BinaryReader(ms);
                var sutMessageDes = SUTMessage.Deserialize(br);

                if (sutMessageDes == sutMessage)
                {
                    Console.WriteLine("Success");
                }
                else
                {
                    Console.WriteLine("Failure");
                }

            }


            //using (var sg = new FileStream("savegame.svg", FileMode.Open))
            //{
            //    using var reader = new BinaryReader(sg);
            //    var singleProp = SinglePropTest.Deserialize(reader);
            //}

            //{
            //    AlternativUser = new SUTMessage.User(),
            //    Complain = new SUTMessage.ComplainBase(),

            //};


            //var count = GetAllIntsOfThisWorld().Count();
            //var serializedSut = JsonConvert.SerializeObject(sut, Formatting.Indented);
            //var res = JsonConvert.DeserializeObject<SUTMessage>(serializedSut);

            //sut.Serialize(new BinaryWriter(new MemoryStream()));

            //var bear = new Bear(true, false);
            //bear.TestEnum = new short[] { 1, 23, 4 };
            //bear.TestDict = new Dictionary<short, short>() { { 2, 1 }, { 3, 23 }, { 12, 4 } };
            //bear.TestPoint = new Rectangle(14, 5, 12, 3);
            //var list = new List<short>();
            ////var bw = new BinaryWriter()
            //var serializedBear = JsonConvert.SerializeObject(bear);
            //var desializedBear = JsonConvert.DeserializeObject<Bear>(serializedBear);
            //var type = desializedBear.TestEnum.GetType();
            //var typeDict = desializedBear.TestDict.GetType();
        }

        static uint i = 0;
        private static IEnumerable<uint> GetAllIntsOfThisWorld()
        {
            while (true)
            {
                yield return i++;
            }
        }

        public abstract class ABear
        {
            public bool IsIceBear { get; }
            public bool IsGummyBear { get; }
            public Rectangle TestPoint { get; set; }

            public IReadOnlyDictionary<short, short> TestDict { get; set; }
            public IReadOnlyList<short> TestEnum { get; set; }
        }

        public class Bear
        {
            public Bear(bool isGummyBear, bool isIceBear)
            {
                IsIceBear = isIceBear;
                IsGummyBear = isGummyBear;
            }

            public bool IsIceBear { get; }
            public bool IsGummyBear { get; }
            public Rectangle TestPoint { get; set; }

            public IReadOnlyDictionary<short, short> TestDict { get; set; }
            public IReadOnlyList<short> TestEnum { get; set; }
        }

    }
}
