using System;

namespace NonSucking.Framework.Extension.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class NoosonIgnoreAttribute : Attribute
    {
    }
}