#nullable enable
using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal class NoosonAttribute : Attribute
    {
        public enum GenerateDefault
        {
            Default = -1,
            No = 0,
            Yes = 1,
        }

        public NoosonAttribute()
        {
        }

        public GenerateDefault GenerateDefaultReader { get; set; } = GenerateDefault.Default;
        public GenerateDefault GenerateDefaultWriter { get; set; } = GenerateDefault.Default;
        public Type[] DirectReaders { get; set; } = Array.Empty<Type>();
        public Type[] DirectWriters { get; set; } = Array.Empty<Type>();
    }
}


