using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    internal class NoosonDynamicTypeAttribute : Attribute
    {
        public NoosonDynamicTypeAttribute(params Type[] possibleTypes)
        {
            PossibleTypes = possibleTypes;
        }
        public Type[] PossibleTypes { get; }
    }
}


