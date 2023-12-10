using System;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class SerializeGenerator : BaseGenerator
{
    public SerializeGenerator(Type writerType, TypeNameCache.TypeConfig typeConfig,
        BaseGeneratorContext.TypeContext valueType)
        : base( new SerializeGeneratorContext(writerType, valueType, gen => gen.Emit(OpCodes.Ldarg_0), true))
    {
        CurrentContext.GetValue = gen => gen.Emit(OpCodes.Ldarg_1);
        CurrentContext.GetValueRef = valueType.Type.IsValueType
            ? gen => gen.Emit(OpCodes.Ldarga, (short)1)
            : CurrentContext.GetValue;

        PushContext(CurrentContext);
        if (!Create(typeConfig))
        {
            throw new TypeSerializerException($"Unable to create dynamic serializer for type config: {typeConfig}");
        }
    }

    public bool GenerateSerialize(SerializeGeneratorContext context, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        PushContext(context);

        ContextMap.TryAdd(context.ValueType, context);

        var res = CustomMethodCallSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  NullableSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  !IsTopLevel && RecursiveCallSerializer.Serialize(this, typeConfig) ||
                  SpecialTypeSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  EnumSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  KnownSimpleTypeSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  UnmanagedTypeSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  MethodCallSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  ArraySerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  ListSerializer.Serialize(this, typeConfig, baseRecursionDepth) ||
                  PublicPropertySerializer.Serialize(this, typeConfig, baseRecursionDepth);

        _ = PopContext();
        return res;
    }

    private bool Create(TypeNameCache.TypeConfig typeConfig)
    {
        var res = GenerateSerialize(CurrentContext, typeConfig);
        if (!res)
            return false;
        Il.Emit(OpCodes.Ret);

        while (Contexts.Count > 1)
            _ = PopContext();
                
        return res;
    }

    public new SerializeGeneratorContext CurrentContext => (SerializeGeneratorContext)base.CurrentContext;
}