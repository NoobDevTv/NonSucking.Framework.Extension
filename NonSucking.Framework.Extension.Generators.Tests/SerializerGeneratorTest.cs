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
            var sutMessage = @"..\..\..\..\DEMO\SUTMessage.cs";
            var text = File.ReadAllText(sutMessage);

            var compilate
            = GeneratorTools.GetGeneratorDiagnostics(new Dictionary<string, string>()
            {
                { sutMessage, text}
            },
            () => new NoosonGenerator());

        }
    }
}
