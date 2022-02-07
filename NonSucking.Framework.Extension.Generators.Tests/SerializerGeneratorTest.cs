using dotVariant.Generator.Test;

using NUnit.Framework;

using System.Collections.Generic;
using System.IO;

namespace NonSucking.Framework.Serialization.Tests
{
    [TestFixture]
    public static class SerializerGeneratorTest
    {
        [Test]
        public static void Debug()
        {
            const string demoPath = @"../../../../DEMO/";
            var secondTestFile = Path.Combine(demoPath, "SecondTestFile.cs");
            var sutMessage = Path.Combine(demoPath, "SUTMessage.cs");
            var iUser = Path.Combine(demoPath, "IUser.cs");
            var message = Path.Combine(demoPath, "Message.cs");
            var singePropTest = Path.Combine(demoPath, "SinglePropTest.cs");
            var recordTestFile = Path.Combine(demoPath, "RecordTestFile.cs");
            var structTestFile = Path.Combine(demoPath, "StructTestFile.cs");
            var nullableTestFile = Path.Combine(demoPath, "NullableTest.cs");
            var listsContainer = Path.Combine(demoPath, "ListsContainer.cs");

            var compilate
            = GeneratorTools.GetGeneratorDiagnostics(new Dictionary<string, string>()
            {
                 { recordTestFile, File.ReadAllText(recordTestFile)},
                 { structTestFile, File.ReadAllText(structTestFile)},
                 { nullableTestFile, File.ReadAllText(nullableTestFile)},
                 { secondTestFile, File.ReadAllText(secondTestFile)},
                 { listsContainer , File.ReadAllText(listsContainer )},
                 { sutMessage, File.ReadAllText(sutMessage)},
                 { iUser, File.ReadAllText(iUser)},
                 { message, File.ReadAllText(message)},
                 { singePropTest, File.ReadAllText(singePropTest)},
            },
            () => new NoosonGenerator());

        }
    }
}
