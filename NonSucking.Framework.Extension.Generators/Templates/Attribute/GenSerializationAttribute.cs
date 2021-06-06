using System;

namespace NonSucking.Framework.Extension.Serialization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional("SerializerGenerator_DEBUG")]
    public class GenSerializationAttribute : Attribute
    {
    }
}
