using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal static class PublicPropertySerializer
{
    private static IEnumerable<(MemberInfo memberInfo, int depth)> OrderProps(IEnumerable<(MemberInfo memberInfo, int depth)> props)
    {
        return props.OrderBy(x =>
                             {
                                 var attr = x.memberInfo.GetCustomAttributesData()
                                     .FirstOrDefault(y => y.AttributeType.FullName == Consts.NoosonOrderAttribute);
                                 return attr == null ? int.MaxValue : (int)attr.ConstructorArguments[0].Value!;
                             }).ThenByDescending(x => x.depth);
    }
    private static IEnumerable<(MemberInfo memberInfo, int depth)> FilterPropsForNotWriteOnly(IEnumerable<(MemberInfo memberInfo, int)> props)
    {
        return props.Where(x => 
                                x.memberInfo is PropertyInfo { GetMethod: not null } ||
                                 x.memberInfo.MemberType == MemberTypes.Field);
    }

    private static NullabilityInfo FixReadability(NullabilityInfo info, bool canRead, bool canWrite)
    {
        return NullabilityHelper.CreateNullability(info.Type,
            canRead || !canWrite ? info.ReadState : info.WriteState,
            !canRead || canWrite ? info.WriteState : info.ReadState,
            info.ElementType, info.GenericTypeArguments);
    }
    private static (Type, NullabilityInfo, PropertyInfo?, FieldInfo?) ExtractInfo(MemberInfo memberInfo)
    {
        return memberInfo switch
        {
            PropertyInfo pi => (pi.PropertyType,
                FixReadability(NullabilityHelper.Context.Create(pi), pi.CanRead, pi.CanWrite), pi, null),
            FieldInfo fi => (fi.FieldType, NullabilityHelper.Context.Create(fi), null, fi),
            _ => throw new ArgumentOutOfRangeException(nameof(memberInfo))
        };
    }

    internal static MethodInfo? GetBaseDeserialize(DeserializeGenerator generator, Type type, bool compareWithOwnSignature, AssemblyNameCache.Names names, List<Type>? requiredParameter = null)
    {
        return Helper.GetFirstMemberWithBase<MethodInfo>(
            type,
            im =>
            {
                var parameters = im.GetParameters();
                return parameters.Length > 1
                       && (requiredParameter is null
                           || parameters.Length == requiredParameter.Count)
                       && !compareWithOwnSignature
                       && im.Name == names.DeserializeOutName
                       && parameters[0].ParameterType.IsAssignableFrom(generator.CurrentContext.ReaderType)
                       && parameters.Skip(1)
                           .All(x => x.IsOut)
                       && (requiredParameter is null
                           || parameters.ForAll(requiredParameter,
                               (a, b) => a.ParameterType == b));
            });
    }

    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;

        var baseSerialize = MethodResolver.GetBestMatch(type, typeConfig.Names.SerializeName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
            new[] { generator.CurrentContext.WriterType }, null);

        var props = Helper.GetMembersWithBase(type, baseSerialize is null ? baseRecursionDepth : 0)
            .Where(x => x.memberInfo is not PropertyInfo p || p.GetIndexParameters().Length == 0);

        props = FilterPropsForNotWriteOnly(props).ToList();

        if (baseSerialize is not null && !baseSerialize.IsAbstract) // Do not generate when done for a different type
        {
            generator.CurrentContext.GetValue!(generator.Il);
            generator.CurrentContext.GetReaderWriter(generator.Il);
            generator.EmitCall(baseSerialize);
        }

        foreach ((var prop, int _) in OrderProps(props))
        {
            var (subType, nullability, pi, fi) = ExtractInfo(prop);
            var subContext =
                generator.CurrentContext.SubContext(
                    new BaseGeneratorContext.TypeContext(subType, nullability),
                    false);
            var v = generator.Il.DeclareLocal(subType);

            var tpConfig = typeConfig;

            if (fi is not null)
            {
                generator.CurrentContext.GetValueRef!(generator.Il);
                generator.Il.Emit(fi.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fi);
                generator.Il.Emit(OpCodes.Stloc, v);
                tpConfig = TypeNameCache.GetTypeConfig(fi);
            }
            else if (pi is not null)
            {
                if (!pi.GetMethod!.IsStatic)
                    generator.CurrentContext.GetValueRef!(generator.Il);
                generator.EmitCall(pi.GetMethod!);
                generator.Il.Emit(OpCodes.Stloc, v);
                tpConfig = TypeNameCache.GetTypeConfig(pi);
            }
            
            subContext.GetValue = g => g.Emit(OpCodes.Ldloc, v);
            subContext.GetValueRef = subType.IsValueType
                                         ? g => g.Emit(OpCodes.Ldloca, v)
                                         : subContext.GetValue;
            
            _ = generator.GenerateSerialize(subContext, tpConfig, baseRecursionDepth - 1);
        }

        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;

        var baseDeserialize = GetBaseDeserialize(generator, type, false, typeConfig.Names);

        var props = Helper.GetMembersWithBase(type, baseDeserialize is null ? baseRecursionDepth : 0)
            .Where(x => x.memberInfo is not PropertyInfo p || p.GetIndexParameters().Length == 0);

        props = FilterPropsForNotWriteOnly(props).ToList();

        var parametersToMatch = baseDeserialize is not null
            ? new List<ParameterInfo>(baseDeserialize.GetParameters().Skip(1))
            : new List<ParameterInfo>();
        var outVariables = baseDeserialize is not null
            ? new LocalBuilder[baseDeserialize.GetParameters().Length - 1]
            : Array.Empty<LocalBuilder>();

        var otherVariables = new List<MemberInfo>();

        var allVariables = new Dictionary<string, (MemberInfo, LocalBuilder)>();

        foreach (var p in props)
        {
            if (baseDeserialize is not null)
            {
                bool foundParameter = false;
                for (int i = parametersToMatch.Count - 1; i >= 0; i--)
                {
                    var outParam = parametersToMatch[i];
                    var t = p.memberInfo switch
                    {
                        FieldInfo fi => fi.FieldType,
                        PropertyInfo pi => pi.PropertyType,
                        _ => null
                    };
                    if (outParam.ParameterType != t || outParam.Name is null || !Helper.MatchIdentifierWithPropName(p.memberInfo.Name, outParam.Name))
                        continue;
                    var v = outVariables[i] = generator.Il.DeclareLocal(outParam.ParameterType.GetElementType()!);
                    parametersToMatch.RemoveAt(i);
                    foundParameter = true;
                    allVariables.Add(p.memberInfo.Name, (p.memberInfo, v));
                    break;
                }

                if (foundParameter)
                    continue;
            }

            otherVariables.Add(p.memberInfo);
        }

        if (baseDeserialize is not null)
        {
            generator.CurrentContext.GetReaderWriter(generator.Il);
            foreach (var p in outVariables)
            {
                generator.Il.Emit(OpCodes.Ldloca, p);
            }
            generator.EmitCall(baseDeserialize);
        }

        foreach (var prop in otherVariables)
        {
            var (subType, nullability, pi, fi) = ExtractInfo(prop);
            var subContext =
                generator.CurrentContext.SubContext(
                    new BaseGeneratorContext.TypeContext(subType, nullability),
                    false);
            var v = generator.Il.DeclareLocal(subType);
            
            
            
            subContext.GetValue = g => g.Emit(OpCodes.Ldloc, v);
            subContext.GetValueRef = subType.IsValueType
                                         ? g => g.Emit(OpCodes.Ldloca, v)
                                         : subContext.GetValue;
            subContext.SetValue = g => g.Emit(OpCodes.Stloc, v);
            
            var tpConfig = typeConfig;
            if (fi is not null)
            {
                tpConfig = TypeNameCache.GetTypeConfig(fi);
            }
            else if (pi is not null)
            {
                tpConfig = TypeNameCache.GetTypeConfig(pi);
            }
            
            _ = generator.GenerateDeserialize(subContext, tpConfig, baseRecursionDepth - 1);


            allVariables.Add(prop.Name, (prop, v));
        }

        var ctorToCall = GetCtorArguments(type, allVariables, out var ctorArgs);

        if (ctorToCall is not null)
        {
            foreach (var (key, arg) in ctorArgs)
            {
                generator.Il.Emit(OpCodes.Ldloc, arg);
                
                allVariables.Remove(key);
            }
            

            generator.Il.Emit(OpCodes.Newobj, ctorToCall);
        
            generator.CurrentContext.SetValue!(generator.Il);
        }

        foreach (var (_, (m, v)) in allVariables)
        {
            if (m is FieldInfo fi)
            {
                generator.CurrentContext.GetValueRef!(generator.Il);
                generator.Il.Emit(OpCodes.Ldloc, v);
                generator.Il.Emit(OpCodes.Stfld, fi);
            }
            else if (m is PropertyInfo { SetMethod: not null } pi)
            {
                generator.CurrentContext.GetValueRef!(generator.Il);
                generator.Il.Emit(OpCodes.Ldloc, v);
                generator.EmitCall(pi.SetMethod);
            }
        }
        

        return true;
    }

    private static IEnumerable<ConstructorInfo> OrderCtors(IEnumerable<ConstructorInfo> ctors)
    {
        return ctors.OrderByDescending(constructor =>
                                           (constructor.GetCustomAttributesData()
                                               .FirstOrDefault(x => x.AttributeType.FullName ==
                                                                    Consts.NoosonPreferredCtorAttribute) is not null
                                               ? 0xFFFF1
                                               : 0) //0xFFFF is the maximum amount of Parameters, so we add an additional one
                                           + constructor.GetParameters().Length);
    }

    internal static ConstructorInfo? GetCtorArguments(Type type, Dictionary<string, (MemberInfo member, LocalBuilder var)> localDeclarations, out List<(string, LocalBuilder)> ctorArguments)
    {
        ctorArguments = new List<(string, LocalBuilder)>();
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
        {
            return null;
        }

        foreach (var constructor in OrderCtors(constructors))
        {
            bool constructorMatch = true;

            foreach (var parameter in constructor.GetParameters())
            {
                var parameterName = parameter.Name;
                if (parameterName is null)
                    break;

                if (parameter.GetCustomAttributesData()
                        .FirstOrDefault(x => x.AttributeType.FullName == Consts.NoosonParameterAttribute) is
                    { } newNameAttribute)
                {
                    parameterName = (string)newNameAttribute.ConstructorArguments[0].Value!;
                }

                var matchedDeclarationKey
                    = localDeclarations
                        .FirstOrDefault(x => Helper.MatchIdentifierWithPropName(parameterName, x.Value.member.Name)).Key;

                if (matchedDeclarationKey is null)
                {
                    constructorMatch = false;
                    break;
                }

                ctorArguments.Add((matchedDeclarationKey, localDeclarations[matchedDeclarationKey].var));
            }


            if (constructorMatch)
            {
                
                
                return constructor;
            }

            ctorArguments.Clear();
        }

        throw new NotSupportedException();
    }

}