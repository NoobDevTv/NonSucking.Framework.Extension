using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    internal class NoosonParameterAttribute : Attribute
    {
        public string PropertyName { get; }

        public NoosonParameterAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}