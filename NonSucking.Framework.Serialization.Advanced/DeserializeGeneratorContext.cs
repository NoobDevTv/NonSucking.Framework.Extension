using System;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

public class DeserializeGeneratorContext : BaseGeneratorContext
{
    public DeserializeGeneratorContext(Type readerType, TypeContext valueType, Action<ILGenerator> getReaderWriter, bool isTopLevel, MethodBuilder method)
        : base(valueType, ContextType.Deserialize, getReaderWriter, isTopLevel, method)
    {
        ReaderType = readerType;
    }

    public DeserializeGeneratorContext(Type readerType, TypeContext valueType, Action<ILGenerator> getReaderWriter, bool isTopLevel)
        : base(readerType, valueType, ContextType.Deserialize, getReaderWriter, isTopLevel)
    {
        ReaderType = readerType;
    }

    public Type ReaderType { get; }
    
    public override DeserializeGeneratorContext SubContext(TypeContext valueType, bool? isTopLevel)
    {
        return new DeserializeGeneratorContext(ReaderType, valueType, GetReaderWriter, isTopLevel ?? IsTopLevel, Method);
    }
}