using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    internal class NoosonParameterAttribute : Attribute
    {
        internal string PropertyName { get; }

        internal NoosonParameterAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}