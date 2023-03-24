#nullable enable
using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal class NoosonConversionAttribute : Attribute
    {
        public NoosonConversionAttribute(Type converter)
        {
            Converter = converter;
        }

        public Type Converter { get; }
        public Type? ConvertTo { get; set; }
    }
}