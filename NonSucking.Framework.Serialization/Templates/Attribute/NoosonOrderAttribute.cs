using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal class NoosonOrderAttribute : Attribute
    {
        public int Order { get; }

        public NoosonOrderAttribute(int order)
        {
            Order = order;
        }
    }
}