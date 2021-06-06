using dotVariant.Generator.Test;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.Generators.Tests
{
    [TestFixture]
    public static class SeriaizerGeneratorTest
    {
        [Test]
        public static void Debug()
        {
            var sutMessage = @"D:\Projekte\Visual 2019\NonSucking.Framework.Extension\DEMO\SUTMessage.cs";
            var text = File.ReadAllText(sutMessage);

            var compilate
            = GeneratorTools.GetGeneratorDiagnostics(new Dictionary<string, string>()
            {
                { sutMessage, text}
            },
            () => new SerializerGenerator());

        }
    }
}
