using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NonSucking.Framework.Serialization.Advanced;

public static class Helper
{
    private static readonly MethodInfo GenericMethodUnmanagedCheck =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences), BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidProgramException($"{nameof(RuntimeHelpers)} does not contain a {nameof(RuntimeHelpers.IsReferenceOrContainsReferences)} method");
    public static bool IsUnmanaged(this Type t)
    {
        try
        {
            return !(bool)GenericMethodUnmanagedCheck.MakeGenericMethod(t).Invoke(null,null)!;
        }
        catch (Exception)
        {
            return false;
        }
    }
    internal static bool ForAll<T, T2>(this IEnumerable<T> list, IList<T2> second, Func<T, T2, bool> check)
    {
        int index = 0;
        foreach (var item in list)
        {
            if (second.Count > index)
            {
                if (!check(item, second[index]))
                    return false;
            }
            else
                return false;
            index++;
        }

        return second.Count == index;
    }
    public static bool IsAssignableFromOpenGeneric(this Type type, Type assignTo)
    {
        return IsAssignableToOpenGeneric(assignTo, type);
    }
    public static bool IsAssignableToOpenGeneric(this Type type, Type assignTo)
    {
        var interfaces = type.GetInterfaces();

        foreach (var i in interfaces)
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == assignTo)
                return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == assignTo)
            return true;

        return type.BaseType is { } baseType && IsAssignableToOpenGeneric(baseType, assignTo);
    }

    public static Type? GetOpenGenericType(this Type type, Type generic)
    {
        Type? currentType = type;
        while (currentType is not null)
        {
            var interfaces = currentType.GetInterfaces();

            foreach (var i in interfaces)
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == generic) return i;
            }

            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == generic)
                return currentType;
            
            currentType = currentType.BaseType;
        }

        return null;
    }

    public static string ToAssemblyNameString(this Type type)
    {
        return type.ToString().Replace(',', '.');
    }
    public static string GetSpecialTypeReadMethodName(TypeCode code)
    {
        switch (code)
        {
            case TypeCode.Boolean:
                return "ReadBoolean";
            case TypeCode.Char:
                return "ReadChar";
            case TypeCode.SByte:
                return "ReadSByte";
            case TypeCode.Byte:
                return "ReadByte";
            case TypeCode.Int16:
                return "ReadInt16";
            case TypeCode.UInt16:
                return "ReadUInt16";
            case TypeCode.Int32:
                return "ReadInt32";
            case TypeCode.UInt32:
                return "ReadUInt32";
            case TypeCode.Int64:
                return "ReadInt64";
            case TypeCode.UInt64:
                return "ReadUInt64";
            case TypeCode.Single:
                return "ReadSingle";
            case TypeCode.Double:
                return "ReadDouble";
            case TypeCode.Decimal:
                return "ReadDecimal";
            case TypeCode.String:
                return "ReadString";
            case TypeCode.Empty:
            case TypeCode.Object:
            case TypeCode.DBNull:
            case TypeCode.DateTime:
            default:
                break;
        }
        
        throw new ArgumentOutOfRangeException(nameof(code), code, null);
    }
    internal static MethodInfo? TryGetRead(DeserializeGenerator generator, Type type, string methodName)
    {
        var readerType = generator.CurrentContext.ReaderType;
        return MethodResolver.GetBestMatch(readerType,
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            Type.EmptyTypes, type);
    }
    internal static MethodInfo? TryGetRead(DeserializeGenerator generator, Type type, TypeCode typeCode, out string methodName)
    {
        methodName = GetSpecialTypeReadMethodName(typeCode);
        return TryGetRead(generator, type, methodName);
    }
    internal static MethodInfo? TryGetRead(DeserializeGenerator generator, Type type, out string methodName)
    {
        return TryGetRead(generator, type, Type.GetTypeCode(type), out methodName);
    }

    internal static MethodInfo? TryGetRead(DeserializeGenerator generator, Type type, TypeCode typeCode)
        => TryGetRead(generator, type, typeCode, out _);

    internal static MethodInfo? TryGetRead(DeserializeGenerator generator, Type type)
        => TryGetRead(generator, type, out _);
    internal static MethodInfo GetRead(DeserializeGenerator generator, Type type, TypeCode typeCode)
    {
        var readerType = generator.CurrentContext.ReaderType;
        return TryGetRead(generator, type, typeCode, out var methodName)
               ?? throw new NotSupportedException($"Reader {readerType} is missing {methodName}({type}) overload.");
    }
    internal static MethodInfo GetRead(DeserializeGenerator generator, Type type)
    {
        var readerType = generator.CurrentContext.ReaderType;
        return TryGetRead(generator, type, out var methodName)
               ?? throw new NotSupportedException($"Reader {readerType} is missing {methodName}({type}) overload.");
    }
    internal static MethodInfo GetRead(DeserializeGenerator generator, Type type, string methodName)
    {
        var readerType = generator.CurrentContext.ReaderType;
        return TryGetRead(generator, type, methodName)
               ?? throw new NotSupportedException($"Reader {readerType} is missing {methodName}({type}) overload.");
    }
    

    internal static MethodInfo? TryGetWrite(SerializeGenerator generator, Type type, string methodName = "Write")
    {
        var writerType = generator.CurrentContext.WriterType;
        return MethodResolver.GetBestMatch(
            writerType,
            methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
            new[] { type }, null);
    }
    internal static MethodInfo GetWrite(SerializeGenerator generator, Type type, string methodName = "Write")
    {
        var writerType = generator.CurrentContext.WriterType;
        return TryGetWrite(generator, type, methodName)
               ?? throw new NotSupportedException($"Writer {writerType} is missing {methodName}({type}) overload.");
    }
    internal static T? GetFirstMemberWithBase<T>(Type? type,
        Func<T, bool> predicate,
        int maxRecursion = int.MaxValue,
        int currentIteration = 0)
    {
        if (type is null)
            return default;
        if (currentIteration++ > maxRecursion)
            return default;

        foreach (var member in type.GetMembers())
        {
            if (member is T t && predicate(t))
                return t;
        }

        return GetFirstMemberWithBase(type.BaseType, predicate, maxRecursion,
            currentIteration);
    }
    internal static IEnumerable<(MemberInfo memberInfo, int depth)> GetMembersWithBase(Type? type,
        int maxRecursion = int.MaxValue, int currentIteration = 0)
    {
        if (currentIteration++ > maxRecursion)
            yield break;

        if (type is null)
            yield break;
        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (member.DeclaringType != type)
                continue;
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (member.MemberType)
            {
                // Exclude CompilerGenerated EqualityContract from serialization process
                case MemberTypes.Property when member.Name == "EqualityContract" && member.GetCustomAttribute<CompilerGeneratedAttribute>() is not null:
                    continue;
                case MemberTypes.Property:
                    if (member.GetCustomAttributesData().FirstOrDefault(x =>
                                                x.AttributeType.FullName == Consts.NoosonIgnoreAttribute) is not null)
                        continue;
                    yield return (member, currentIteration);
                    break;
                case MemberTypes.Field:
                    if (member.GetCustomAttributesData().FirstOrDefault(x =>
                                                                            x.AttributeType.FullName == Consts.NoosonIncludeAttribute) is null)
                        continue;
                    yield return (member, currentIteration);
                    break;
            }
        }

        foreach (var item in GetMembersWithBase(type.BaseType, maxRecursion, currentIteration))
        {
            yield return item;
        }
    }
    internal static bool MatchIdentifierWithPropName(string identifier, string parameterName)
    {
        var index = identifier.IndexOf(Consts.LocalVariableSuffix, StringComparison.Ordinal);
        if (index > -1)
            identifier = identifier.Remove(index);
        index = parameterName.IndexOf(Consts.LocalVariableSuffix, StringComparison.Ordinal);
        if (index > -1)
            parameterName = parameterName.Remove(index);

        return char.ToLowerInvariant(identifier[0]) == char.ToLowerInvariant(parameterName[0])
               && string.Equals(identifier[1..], parameterName[1..]);
    }
    internal static MethodInfo? GetMethodIncludingInterfaces(Type type, string name, BindingFlags bindingFlags)
    {
        var res = type.GetMethod(name, bindingFlags);
        if (res is not null)
            return res;
        
        return GetMethodInInterfaces(type, name, bindingFlags);
    }

    internal static MethodInfo? GetMethodInInterfaces(Type type, string name, BindingFlags bindingFlags)
    {
        foreach (var i in type.GetInterfaces())
        {
            var res = i.GetMethod(name, bindingFlags);
            if (res is not null)
                return res;
        }

        return null;
    }
    
    
    internal static PropertyInfo? GetPropertyIncludingInterfaces(Type type, string name, BindingFlags bindingFlags)
    {
        var res = type.GetProperty(name, bindingFlags);
        if (res is not null)
            return res;
        foreach (var i in type.GetInterfaces())
        {
            res = i.GetProperty(name, bindingFlags);
            if (res is not null)
                return res;
        }

        return null;
    }
}