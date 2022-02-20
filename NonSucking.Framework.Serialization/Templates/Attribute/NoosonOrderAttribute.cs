using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal class NoosonOrderAttribute : Attribute
    {
        internal int Order { get; }

        internal NoosonOrderAttribute(int order)
        {
            Order = order;
        }
    }
}