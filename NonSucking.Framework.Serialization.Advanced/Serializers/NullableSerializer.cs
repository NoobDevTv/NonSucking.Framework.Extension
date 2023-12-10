using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal static class NullableSerializer
{
    private static NullabilityInfo RemoveNullability(NullabilityInfo info)
    {
        return NullabilityHelper.CreateNullability(info.Type, NullabilityState.NotNull,
            NullabilityState.NotNull, info.ElementType, info.GenericTypeArguments);
    }
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType;
        if (type.NullabilityInfo.ReadState == NullabilityState.NotNull)
            return false;

        var writeBool = Helper.GetWrite(generator, typeof(bool));
        var nullableType = type.Type;

        if (type.Type.IsValueType)
        {
            nullableType = type.Type.GetGenericArguments()[0];
            generator.CurrentContext.GetValueRef!(generator.Il);
            var getHasValue = type.Type.GetProperty("HasValue")!.GetMethod!;
            generator.EmitCall(getHasValue);
        }
        else
        {
            generator.CurrentContext.GetValue!(generator.Il);
            generator.Il.Emit(OpCodes.Ldnull);
            generator.Il.Emit(OpCodes.Cgt_Un);
        }

        var isNotNullVar = generator.Il.DeclareLocal(typeof(bool));
        generator.Il.Emit(OpCodes.Stloc, isNotNullVar);
        
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Ldloc, isNotNullVar);
        
        generator.Il.Emit(OpCodes.Callvirt, writeBool);
        generator.Il.Emit(OpCodes.Ldloc, isNotNullVar);
        
        
        
        generator.EmitIf(OpCodes.Brfalse, gen =>
                                          {
                                              var subContext =
                                                  gen.CurrentContext.SubContext(
                                                      new BaseGeneratorContext.TypeContext(nullableType, RemoveNullability(type.NullabilityInfo)),
                                                      null);
                                              if (type.Type.IsValueType)
                                              {
                                                  var v = gen.Il.DeclareLocal(nullableType);
                                                  var getValue = type.Type.GetProperty("Value")!.GetMethod!;
                                                  
                                                  gen.CurrentContext.GetValueRef!(gen.Il);
                                                  gen.EmitCall(getValue);
                                                  gen.Il.Emit(OpCodes.Stloc, v);

                                                  subContext.GetValue = g => g.Emit(OpCodes.Ldloc, v);
                                                  subContext.GetValueRef = g => g.Emit(OpCodes.Ldloca, v);
                                              }
                                              else
                                              {
                                                  subContext.GetValue = gen.CurrentContext.GetValue;
                                                  subContext.GetValueRef = gen.CurrentContext.GetValueRef;
                                              }
                                              

                                              _ = ((SerializeGenerator)gen).GenerateSerialize((SerializeGeneratorContext)subContext, typeConfig, baseRecursionDepth);
                                          }, null);


        
        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType;
        if (type.NullabilityInfo.WriteState == NullabilityState.NotNull)
            return false;

        var nullableType = type.Type;
        if (type.Type.IsValueType)
        {
            nullableType = type.Type.GetGenericArguments()[0];
        }
        
        var readBool = Helper.GetRead(generator, typeof(bool));

        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Callvirt, readBool);

        
        generator.EmitIf(OpCodes.Brfalse, gen =>
                                         {
                                             var subContext =
                                                 gen.CurrentContext.SubContext(
                                                     new BaseGeneratorContext.TypeContext(nullableType, RemoveNullability(type.NullabilityInfo)),
                                                     null);

                                             if (type.Type.IsValueType)
                                             {
                                                 var v = gen.Il.DeclareLocal(nullableType);
                                                 
                                                 subContext.GetValue = g => g.Emit(OpCodes.Ldloc, v);
                                                 subContext.GetValueRef = g => g.Emit(OpCodes.Ldloca, v);
                                                 subContext.SetValue = g => g.Emit(OpCodes.Stloc, v);
                                             }
                                             else
                                             {
                                                 subContext.SetValue = gen.CurrentContext.SetValue;
                                                 subContext.GetValue = gen.CurrentContext.GetValue;
                                                 subContext.GetValueRef = gen.CurrentContext.GetValueRef;
                                             }

                                             _ = ((DeserializeGenerator)gen).GenerateDeserialize((DeserializeGeneratorContext)subContext, typeConfig);
                                             
                                             if (type.Type.IsValueType)
                                             {
                                                 subContext.GetValue!(gen.Il);
                                                 gen.Il.Emit(OpCodes.Newobj, type.Type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, new []{ nullableType })!);
                                                 gen.CurrentContext.SetValue!(gen.Il);
                                             }
                                         }, null);

        return true;
    }
}