using System;

namespace NonSucking.Framework.Extension.Serialization
{
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public class NoosonPreferredCtorAttribute : Attribute
    {
    }
}