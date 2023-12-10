using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal static class ListSerializer
{
    private static NullabilityInfo CreateListNullability(NullabilityInfo info)
    {
        return NullabilityHelper.CreateNullability(info.Type, info.ReadState,
            info.ReadState, info.ElementType, info.GenericTypeArguments);
    }

    private static NullabilityInfo GetItemNullability(Type type)
    {
        var info = NullabilityHelper.Context.Create(
            Helper.GetMethodIncludingInterfaces(type, "GetEnumerator", BindingFlags.Instance | BindingFlags.Public)!.ReturnType.GetProperty("Current")!);
        return CreateListNullability(info);
    }
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;

        var readonlyCollection = type.GetOpenGenericType(typeof(IReadOnlyCollection<>));
        if (readonlyCollection is null)
            return false;
        var elementType = readonlyCollection.GenericTypeArguments[0];

        var getCountProp = Helper.GetPropertyIncludingInterfaces(type, "Count",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                           ?? throw new InvalidOperationException($"{type} is a List but does not have a valid Count property.");
        
        if (getCountProp.GetMethod is null)
            throw new InvalidOperationException($"{type} is a List but does not have a valid Count getter.");

        var inheritanceDepth = GetInheritanceDepth(type);

        if (inheritanceDepth > -1)
        {
            var subContext =
                generator.CurrentContext.SubContext(generator.CurrentContext.ValueType,
                    null);
            subContext.SetValue = generator.CurrentContext.SetValue;
            subContext.GetValue = generator.CurrentContext.GetValue;
            subContext.GetValueRef = generator.CurrentContext.GetValueRef;

            generator.PushContext(subContext);
            
            _ = PublicPropertySerializer.Serialize(generator, typeConfig, inheritanceDepth);

            _ = generator.PopContext();
        }

        var writeInt = Helper.GetWrite(generator, typeof(int));
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.CurrentContext.GetValueRef!(generator.Il);
        generator.Il.Emit(OpCodes.Callvirt, getCountProp.GetMethod);
        generator.Il.Emit(OpCodes.Callvirt, writeInt);

        generator.EmitForEach(type, gen => gen.CurrentContext.GetValue!(gen.Il),
            (gen, cont, br, item) =>
            {
                var subContext =
                    generator.CurrentContext.SubContext(new BaseGeneratorContext.TypeContext(elementType, GetItemNullability(type)),
                        null);
                subContext.GetValue = g => g.Emit(OpCodes.Ldloc, item);
                subContext.GetValueRef = elementType.IsValueType
                    ? g => g.Emit(OpCodes.Ldloca, item)
                    : subContext.GetValue;

                var tpConf = TypeNameCache.GetTypeConfig(elementType);

                _ = generator.GenerateSerialize(subContext, tpConf);
            });

        return true;
    }

    private static ConstructorInfo? GetCtor(Type type)
    {
        return type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, new[] { typeof(int) })
               ?? type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
    }

    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;

        var readonlyCollection = type.GetOpenGenericType(typeof(IReadOnlyCollection<>));
        if (readonlyCollection is null)
            return false;
        var elementType = readonlyCollection.GenericTypeArguments[0];


        var inheritanceDepth = GetInheritanceDepth(type);

        if (inheritanceDepth > -1)
        {
            var subContext =
                generator.CurrentContext.SubContext(generator.CurrentContext.ValueType,
                    null);
            subContext.SetValue = generator.CurrentContext.SetValue;
            subContext.GetValue = generator.CurrentContext.GetValue;
            subContext.GetValueRef = generator.CurrentContext.GetValueRef;

            generator.PushContext(subContext);
            
            _ = PublicPropertySerializer.Deserialize(generator, typeConfig, inheritanceDepth);

            _ = generator.PopContext();
        }

        var readInt = Helper.GetRead(generator, typeof(int));
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Callvirt, readInt);

        var countVar = generator.Il.DeclareLocal(typeof(int));
        generator.Il.Emit(OpCodes.Stloc, countVar);

        ConstructorInfo? ctor = null;
        Type? baseInterfaceType = null;

        var elementTypeGenParams = new[] { elementType };
        if (type is { IsAbstract: false, IsInterface: false })
        {
            ctor = GetCtor(type);
            baseInterfaceType = type;
        }
        if (typeof(IReadOnlySet<>).MakeGenericType(elementTypeGenParams) is {} setType && setType.IsAssignableFrom(type))
        {
            ctor ??= GetCtor(setType);
            baseInterfaceType = setType;
        }
        else
        {
            if (elementType.IsGenericType && elementType.IsAssignableToOpenGeneric(typeof(KeyValuePair<,>)))
            {
                var keyType = elementType.GenericTypeArguments[0];
                var valueType = elementType.GenericTypeArguments[1];
                var genericParams = new[] { keyType, valueType };
                if (typeof(IDictionary).MakeGenericType(genericParams) is {} dictType && dictType.IsAssignableFrom(type)
                    || typeof(IReadOnlyDictionary<,>).MakeGenericType(genericParams) is {} readOnlyDictType && readOnlyDictType.IsAssignableFrom(type))
                {
                    ctor ??= GetCtor(typeof(Dictionary<,>).MakeGenericType(genericParams));
                    
                    baseInterfaceType = typeof(ICollection).MakeGenericType(elementTypeGenParams);
                }
            }

            if (typeof(IList).MakeGenericType(elementTypeGenParams) is {} listType && listType.IsAssignableFrom(type)
                || typeof(IReadOnlyCollection<>).MakeGenericType(elementTypeGenParams) is {} readOnlyListType && readOnlyListType.IsAssignableFrom(type))
            {
                ctor ??= GetCtor(typeof(List<>).MakeGenericType(elementTypeGenParams));
                baseInterfaceType = typeof(ICollection).MakeGenericType(elementTypeGenParams);
            }
        }

        if (ctor is null)
        {
            throw new NotSupportedException();
        }

        var tempListVar = generator.Il.DeclareLocal(ctor.DeclaringType!);
        if (ctor.GetParameters().Length == 1)
        {
            generator.Il.Emit(OpCodes.Ldloc, countVar);
        }
        generator.Il.Emit(OpCodes.Newobj, ctor);

        generator.Il.Emit(OpCodes.Stloc, tempListVar);

        MethodInfo? GetAddMethod(Type? t)
        {
            if (t is null)
                return null;
            return t.GetMethod(nameof(IList.Add),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic, elementTypeGenParams)
                ?? t.GetMethod(nameof(Stack.Push),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic, elementTypeGenParams)
                ?? t.GetMethod(nameof(Queue.Enqueue),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic, elementTypeGenParams);
        }

        var addMethod = GetAddMethod(tempListVar.LocalType)
                        ?? GetAddMethod(baseInterfaceType);

        if (addMethod is null)
            throw new NotSupportedException();

        generator.EmitFor(
            g =>
            {
                var counter = g.Il.DeclareLocal(typeof(int));
                g.Il.Emit(OpCodes.Ldc_I4_0);
                g.Il.Emit(OpCodes.Stloc, counter);
                return counter;
            },
            (g, counter) =>
            {
                g.Il.Emit(OpCodes.Ldloc, counter!);
                g.Il.Emit(OpCodes.Ldloc, countVar);
                g.Il.Emit(OpCodes.Clt);
            },
            (g, counter) =>
            {
                g.EmitIncrement(counter!);
            },
            (g, _, _, _) =>
            {

                var item = g.Il.DeclareLocal(elementType);

                var subContext =
                    generator.CurrentContext.SubContext(new BaseGeneratorContext.TypeContext(elementType, GetItemNullability(type)),
                        null);
                subContext.GetValue = gen => gen.Emit(OpCodes.Ldloc, item);
                subContext.GetValueRef = elementType.IsValueType
                                ? gen => gen.Emit(OpCodes.Ldloca, item)
                                : subContext.GetValue;
                subContext.SetValue = gen => gen.Emit(OpCodes.Stloc, item);

                if (elementType.IsPrimitive || elementType.IsPointer)
                {
                    g.Il.Emit(OpCodes.Ldc_I4_0);
                    subContext.SetValue(g.Il);
                }
                else if (elementType.IsValueType)
                {
                    subContext.GetValueRef(g.Il);
                    g.Il.Emit(OpCodes.Initobj, elementType);
                }
                else
                {
                    g.Il.Emit(OpCodes.Ldnull);
                    subContext.SetValue(g.Il);
                }

                var tpConf = TypeNameCache.GetTypeConfig(elementType);

                _ = generator.GenerateDeserialize(subContext, tpConf);

                g.Il.Emit(OpCodes.Ldloc, tempListVar);
                subContext.GetValue!(g.Il);
                g.EmitCall(addMethod);
                if (addMethod.ReturnType != typeof(void))
                    g.Il.Emit(OpCodes.Pop);

            });

        generator.Il.Emit(OpCodes.Ldloc, tempListVar);
        generator.CurrentContext.SetValue!(generator.Il);
        
        return true;
    }

    private static int GetInheritanceDepth(Type? type, int lastAmount = -1)
    {
        while (type is not null && !type.IsArray)
        {
            if (type.GetInterfaces().Any(x => x == typeof(IEnumerable)))
                return lastAmount;
            type = type.BaseType;
            lastAmount = ++lastAmount;
        }

        return lastAmount;
    }
}