using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DEMO
{
    class Program
    {

        static void Main(string[] args)
        {
            //var sut = new SUTMessage()
            //{
            //    AlternativUser = new SUTMessage.User(),
            //    Complain = new SUTMessage.ComplainBase(),

            //};


            //var count = GetAllIntsOfThisWorld().Count();
            //var serializedSut = JsonConvert.SerializeObject(sut, Formatting.Indented);
            //var res = JsonConvert.DeserializeObject<SUTMessage>(serializedSut);

            //sut.Serialize(new BinaryWriter(new MemoryStream()));

            var bear = new Bear(true, false);
            bear.TestEnum = new short[] { 1, 23, 4 };
            bear.TestDict = new Dictionary<short, short>() { { 2, 1 }, { 3, 23 }, { 12, 4 } };
            bear.TestPoint = new Rectangle(14, 5, 12, 3);
            var list = new List<short>();
            //var bw = new BinaryWriter()
            var serializedBear = JsonConvert.SerializeObject(bear);
            var desializedBear = JsonConvert.DeserializeObject<Bear>(serializedBear);
            var type = desializedBear.TestEnum.GetType();
            var typeDict = desializedBear.TestDict.GetType();
        }

        static uint i = 0;
        private static IEnumerable<uint> GetAllIntsOfThisWorld()
        {
            while (true)
            {
                yield return i++;
            }
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
