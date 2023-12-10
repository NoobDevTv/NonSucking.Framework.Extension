using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class ArraySerializer
{
    private static readonly MethodInfo GetLength = MethodResolver.GetBestMatch(typeof(Array), "GetLength",
        BindingFlags.Instance | BindingFlags.Public,
        new[] { typeof(int) }, typeof(int))
        ?? throw new ArgumentNullException("Array.GetLength not found", (Exception?)null);

    private static void CreateNestedFor<T>(int rank, T generator, LocalBuilder[] loopVariables, LocalBuilder[] countVariables, Action<T> body)
        where T : BaseGenerator
    {
        var previousMethod = body;
        for (int i = rank - 1; i >= 0; i--)
        {
            var iCopyGen = i;
            var previousMethodCopyGen = previousMethod;
            previousMethod = gen =>
                             {
                                 var iCopyFor = iCopyGen;
                                 var previousMethodCopyFor = previousMethodCopyGen;
                                 gen.EmitFor(
                                     g =>
                                     {
                                         g.Il.Emit(OpCodes.Ldc_I4_0);
                                         g.Il.Emit(OpCodes.Stloc, loopVariables[iCopyFor]);
                                         return loopVariables[iCopyFor];
                                     },
                                     (g, var) =>
                                     {
                                         g.Il.Emit(OpCodes.Ldloc, var!);
                                         g.Il.Emit(OpCodes.Ldloc, countVariables[iCopyFor]);
                                         g.Il.Emit(OpCodes.Clt);
                                     },
                                     (g, var) =>
                                     {
                                         g.EmitIncrement(var!);
                                     },
                                     (g, _, _, _) =>
                                     {
                                         previousMethodCopyFor((T)g);
                                     });
                             };
        }

        previousMethod(generator);
    }
    
    
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType;
        if (!type.Type.IsArray)
            return false;
        var rank = type.Type.GetArrayRank();
        if (rank < 1)
            return false;
        var elementType = type.Type.GetElementType()
                          ?? throw new InvalidOperationException($"{type.Type} is an array but array element type could not be resolved");

        var getterMethod = MethodResolver.GetBestMatch(type.Type, "Get", BindingFlags.Instance | BindingFlags.Public,
            Enumerable.Repeat(typeof(int), rank).ToArray(), elementType)
                           ?? throw new InvalidOperationException($"{type.Type} is an array but get method could not be resolved");

        var loopVariables = new LocalBuilder[rank];
        var countVariables = new LocalBuilder[rank];
        void ForBody(SerializeGenerator gen)
        {
            gen.CurrentContext.GetValue!(gen.Il);
            for (int i = 0; i < rank; i++)
            {
                gen.Il.Emit(OpCodes.Ldloc, loopVariables[i]);
            }
            if (rank == 1)
                gen.Il.Emit(OpCodes.Ldelem, elementType);
            else
                gen.Il.Emit(OpCodes.Callvirt, getterMethod);
            var elementVar = gen.Il.DeclareLocal(elementType);
            gen.Il.Emit(OpCodes.Stloc, elementVar);
            
            var subContext =
                gen.CurrentContext.SubContext(
                    new BaseGeneratorContext.TypeContext(elementType, type.NullabilityInfo.ElementType!),
                    null);
            subContext.GetValue = _ => gen.Il.Emit(OpCodes.Ldloc, elementVar);
            subContext.GetValueRef = elementType.IsValueType
                ? _ => gen.Il.Emit(OpCodes.Ldloca, elementVar)
                : subContext.GetValue;

            var tpConfig = TypeNameCache.GetTypeConfig(elementType);
            
            _ = gen.GenerateSerialize(subContext, tpConfig);
        }
        
        var writeInt = Helper.GetWrite(generator, typeof(int));

        for (int i = 0; i < rank; i++)
        {
            var countVar = generator.Il.DeclareLocal(typeof(int));
            generator.CurrentContext.GetValue!(generator.Il);
            if (rank == 1)
            {
                var length = type.Type.GetProperty("Length")!;
                generator.Il.Emit(OpCodes.Callvirt, length.GetMethod!);
            }
            else
            {
                generator.Il.Emit(OpCodes.Ldc_I4, i);
                generator.Il.Emit(OpCodes.Callvirt, GetLength);
            }
            generator.Il.Emit(OpCodes.Stloc, countVar);

            generator.CurrentContext.GetReaderWriter(generator.Il);
            generator.Il.Emit(OpCodes.Ldloc, countVar);
            generator.Il.Emit(OpCodes.Callvirt, writeInt);
            
            
            countVariables[i] = countVar;

            loopVariables[i] = generator.Il.DeclareLocal(typeof(int));
        }

        CreateNestedFor(rank, generator, loopVariables, countVariables, ForBody);
        

        return true;
    }
    
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType;
        if (!type.Type.IsArray)
            return false;
        var rank = type.Type.GetArrayRank();
        if (rank < 1)
            return false;
        
        var elementType = type.Type.GetElementType()
                          ?? throw new InvalidOperationException($"{type.Type} is an array but array element type could not be resolved");

        var setterMethod = MethodResolver.GetBestMatch(type.Type, "Set", BindingFlags.Instance | BindingFlags.Public,
            Enumerable.Repeat(typeof(int), rank).Append(elementType).ToArray(), null)
                           ?? throw new InvalidOperationException($"{type.Type} is an array but set method could not be resolved");

        var loopVariables = new LocalBuilder[rank];
        var countVariables = new LocalBuilder[rank];
        
        void ForBody(DeserializeGenerator gen)
        {
            var subContext =
                gen.CurrentContext.SubContext(
                    new BaseGeneratorContext.TypeContext(elementType, type.NullabilityInfo.ElementType!),
                    null);

            var bodyResVar = gen.Il.DeclareLocal(elementType);
            subContext.SetValue = g => g.Emit(OpCodes.Stloc, bodyResVar);
            subContext.GetValue = g => g.Emit(OpCodes.Ldloc, bodyResVar);
            subContext.GetValueRef = elementType.IsValueType 
                                 ? g => g.Emit(OpCodes.Ldloca, bodyResVar)
                                 : subContext.GetValue;

            var tpConfig = TypeNameCache.GetTypeConfig(elementType);
            _ = gen.GenerateDeserialize(subContext, tpConfig);

            gen.CurrentContext.GetValue!(gen.Il);
            for (int i = 0; i < rank; i++)
            {
                gen.Il.Emit(OpCodes.Ldloc, loopVariables[i]);
            }

            gen.Il.Emit(OpCodes.Ldloc, bodyResVar);
            if (rank == 1)
                gen.Il.Emit(GetStElem(elementType));
            else
                gen.Il.Emit(OpCodes.Callvirt, setterMethod);

        }

        // var resArray = generator.IL.DeclareLocal(type);
        // generator.CurrentContext.GetValue = (g) => g.Emit(OpCodes.Ldloc, resArray);
        //
        var readInt = Helper.GetRead(generator, typeof(int));

        for (int i = 0; i < rank; i++)
        {
            var countVar = generator.Il.DeclareLocal(typeof(int));
            generator.CurrentContext.GetReaderWriter(generator.Il);
            generator.Il.Emit(OpCodes.Callvirt, readInt);
            generator.Il.Emit(OpCodes.Stloc, countVar);
            
            countVariables[i] = countVar;

            loopVariables[i] = generator.Il.DeclareLocal(typeof(int));
        }

        for (int i = 0; i < rank; i++)
        {
            generator.Il.Emit(OpCodes.Ldloc, countVariables[i]);
        }

        if (rank == 1)
        {
            generator.Il.Emit(OpCodes.Newarr, elementType);
        }
        else
        {
            var arrayCtor = type.Type.GetConstructor(Enumerable.Repeat(typeof(int), rank).ToArray())
                ?? throw new InvalidOperationException($"{type.Type} is an array but constructor could not be resolved");

            generator.Il.Emit(OpCodes.Newobj, arrayCtor);
        }

        generator.CurrentContext.SetValue!(generator.Il);
        

        CreateNestedFor(rank, generator, loopVariables, countVariables, ForBody);
        //
        // generator.CurrentContext.GetValue = g => g.Emit(OpCodes.Ldloc, resArray);
        // generator.CurrentContext.GetValue = g => g.Emit(OpCodes.Ldloc, resArray);

        return true;
    }

    private static OpCode GetStElem(Type type)
    {
        if (!type.IsValueType)
            return OpCodes.Stelem_Ref;
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Boolean:
                return OpCodes.Stelem_I1;
            case TypeCode.Char:
                return OpCodes.Stelem_I2;
            case TypeCode.Int32:
            case TypeCode.UInt32:
                return OpCodes.Stelem_I4;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return OpCodes.Stelem_I8;
            case TypeCode.Single:
                return OpCodes.Stelem_R4;
            case TypeCode.Double:
                return OpCodes.Stelem_R8;
        }

        if (type == typeof(IntPtr) || type == typeof(UIntPtr))
            return OpCodes.Stelem_I;

        return OpCodes.Stelem;
    }
}