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
        public NoosonAttribute(GenerateDefault generateDefaultReader = GenerateDefault.Default, GenerateDefault generateDefaultWriter = GenerateDefault.Default, Type[]? directReaders = null, Type[]? directWriters = null)
        {
            GenerateDefaultReader = generateDefaultReader;
            GenerateDefaultWriter = generateDefaultWriter;

            DirectReaders = directReaders ?? Array.Empty<Type>();
            DirectWriters = directWriters ?? Array.Empty<Type>();
        }
        public GenerateDefault GenerateDefaultReader { get; }
        public GenerateDefault GenerateDefaultWriter { get; }
        public Type[] DirectReaders { get; }
        public Type[] DirectWriters { get; }
    }
}


