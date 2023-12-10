using System;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class DeserializeGenerator : BaseGenerator
{
    public DeserializeGenerator(Type readerType, TypeNameCache.TypeConfig typeConfig, BaseGeneratorContext.TypeContext valueType, BaseGeneratorContext.ContextType contextType)
        : base(new DeserializeGeneratorContext(readerType, valueType, gen => gen.Emit(OpCodes.Ldarg_0), true))
    {
        if (contextType == BaseGeneratorContext.ContextType.Serialize)
        {
            CurrentContext.GetValue = gen => gen.Emit(OpCodes.Ldarg_1);
            CurrentContext.GetValueRef = valueType.Type.IsValueType
                ? gen => gen.Emit(OpCodes.Ldarga, (short)1)
                : CurrentContext.GetValue;
        }
        else
        {
            var resVal = CurrentContext.Il.DeclareLocal(valueType.Type);
            CurrentContext.SetValue = gen => gen.Emit(OpCodes.Stloc, resVal);
            CurrentContext.GetValue = gen => gen.Emit(OpCodes.Ldloc, resVal);
            CurrentContext.GetValueRef = valueType.Type.IsValueType
                ? gen => gen.Emit(OpCodes.Ldloca, resVal)
                : CurrentContext.GetValue;
        }
        PushContext(CurrentContext);
        if (!Create(typeConfig))
        {
            throw new TypeSerializerException($"Unable to create dynamic deserializer for type config: {typeConfig}");
        }
    }
    
    public bool GenerateDeserialize(DeserializeGeneratorContext context, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        ContextMap.TryAdd(context.ValueType, context);
        PushContext(context);
        ContextMap.TryAdd(context.ValueType, context);

        var res = CustomMethodCallSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  NullableSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  !IsTopLevel && RecursiveCallSerializer.Deserialize(this, typeConfig) ||
                  SpecialTypeSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  EnumSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  KnownSimpleTypeSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  UnmanagedTypeSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  MethodCallSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  ArraySerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  ListSerializer.Deserialize(this, typeConfig, baseRecursionDepth) ||
                  PublicPropertySerializer.Deserialize(this, typeConfig, baseRecursionDepth);

        _ = PopContext();
        return res;
    }

    private bool Create(TypeNameCache.TypeConfig typeConfig)
    {
        var res = GenerateDeserialize(CurrentContext, typeConfig);
        if (!res)
            return false;
        CurrentContext.GetValue!(Il);
        Il.Emit(OpCodes.Ret);
        while (Contexts.Count > 1)
            _ = PopContext();
        return res;
    }
    
    public new DeserializeGeneratorContext CurrentContext => (DeserializeGeneratorContext)base.CurrentContext;
}