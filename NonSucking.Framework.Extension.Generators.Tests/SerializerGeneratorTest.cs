using dotVariant.Generator.Test;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonSucking.Framework.Serialization.Tests
{
    [TestFixture]
    public static class SerializerGeneratorTest
    {
        [Test]
        public static void Debug()
        {
            string demoPath= Path.Combine("..", "..", "..", "..", "DEMO");
            var sutMessage = Path.Combine(demoPath, "SUTMessage.cs");
            var iUser = Path.Combine(demoPath, "IUser.cs");
            var message = Path.Combine(demoPath, "Message.cs");
            var singePropTest = Path.Combine(demoPath, "SinglePropTest.cs");

            var compilate
            = GeneratorTools.GetGeneratorDiagnostics(new Dictionary<string, string>()
            {
                { sutMessage, File.ReadAllText(sutMessage)},
                { iUser, File.ReadAllText(iUser)},
                { message, File.ReadAllText(message)},
                { singePropTest, File.ReadAllText(singePropTest)},
            },
            () => new NoosonGenerator());

        }
    }
}
