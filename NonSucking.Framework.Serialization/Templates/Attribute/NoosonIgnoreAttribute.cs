using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal class NoosonIgnoreAttribute : Attribute
    {
    }
}