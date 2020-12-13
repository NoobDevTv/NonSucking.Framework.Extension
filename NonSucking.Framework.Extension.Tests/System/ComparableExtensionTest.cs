using NonSucking.Framework.Extension.System;
using NUnit.Framework;

namespace NonSucking.Framework.Extension.Tests.System
{
    [TestFixture]
    [TestOf(typeof(ComparableExtension))]
    public sealed class ComparableExtensionTest
    {
        [TestCase(0, 0, 1, 0, Description = "Zero")]
        [TestCase(20, 0, 100, 20, Description = "Positive value in between")]
        [TestCase(200, 0, 100, 100, Description = "Positive maximum")]
        [TestCase(0, 10, 100, 10, Description = "Positive minimum")]
        [TestCase(-25, -50, -10, -25, Description = "Negative value in between")]
        [TestCase(-1, -50, -10, -10, Description = "Negative maximum")]
        [TestCase(-100, -50, -10, -50, Description = "Negative minimum")]
        public void Clamp_Integer(int value, int min, int max, int result)
        {
            Assert.That(value.Clamp(min, max), Is.EqualTo(result));
        }

        [TestCase(0.0, 0.0, 1.0, 0.0, Description = "Zero")]
        [TestCase(20.2, 0.0, 100.1, 20.2, Description = "Positive value in between")]
        [TestCase(200.2, 0.0, 100.1, 100.1, Description = "Positive maximum")]
        [TestCase(0.0, 10.1, 100.1, 10.1, Description = "Positive minimum")]
        [TestCase(-25.2, -50.5, -10.1, -25.2, Description = "Negative value in between")]
        [TestCase(-1.1, -50.5, -10.1, -10.1, Description = "Negative maximum")]
        [TestCase(-100.1, -50.5, -10.1, -50.5, Description = "Negative minimum")]
        public void Clamp_Double(double value, double min, double max, double result)
        {
            Assert.That(value.Clamp(min, max), Is.EqualTo(result));
        }
    }
}
