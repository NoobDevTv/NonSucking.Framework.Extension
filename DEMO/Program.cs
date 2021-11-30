using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;

using static DEMO.SUTMessage;

namespace DEMO
{
    class Program
    {

        static void Main(string[] args)
        {

            var sutMessage = new SUTMessage
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
                Countings = new() { 1,3,4,8,9,4564,231,8976,687},
                countPositions = 123121,
                Position = new Point(12,99),
                Positions = new[] { new Point(777,888), new Point(7770, 4594), new Point(445678, 42), new Point(6456, 4567), },
                ReadOnlyCountingsButSetable = new List<short> { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22},
                Right = AccessRight.A,
                Text = "Just a random text",
                Type = 897987414,
                UsersList = new List<User>() { new User() { Name= "1User"}, new User() { Name = "2User" }, new User() { Name = "3User" }, new User() { Name = "4User" } },
                X = 123123,
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
