using System;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

public class SerializeGeneratorContext : BaseGeneratorContext
{
    public SerializeGeneratorContext(Type writerType, TypeContext valueType, Action<ILGenerator> getReaderWriter, bool isTopLevel, MethodBuilder method)
        : base(valueType, ContextType.Serialize, getReaderWriter, isTopLevel, method)
    {
        WriterType = writerType;
    }

    public SerializeGeneratorContext(Type writerType, TypeContext valueType, Action<ILGenerator> getReaderWriter, bool isTopLevel)
        : base(writerType, valueType, ContextType.Serialize, getReaderWriter, isTopLevel)
    {
        WriterType = writerType;
    }
    public Type WriterType { get; }

    public override SerializeGeneratorContext SubContext(TypeContext valueType, bool? isTopLevel)
    {
        return new SerializeGeneratorContext(WriterType, valueType, GetReaderWriter, isTopLevel ?? IsTopLevel, Method);
    }
}