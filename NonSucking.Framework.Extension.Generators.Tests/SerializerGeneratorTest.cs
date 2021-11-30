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
            const string demoPath= @"..\..\..\..\DEMO\";
            var sutMessage = @$"{demoPath}SUTMessage.cs";
            var iUser = @$"{demoPath}IUser.cs";
            var message = @$"{demoPath}Message.cs";
            var singePropTest = @$"{demoPath}SinglePropTest.cs";

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
