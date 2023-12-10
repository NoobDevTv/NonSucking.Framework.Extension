#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

namespace NonSucking.Framework.Serialization;

internal class AssemblyNameCache
{
    internal struct Names
    {
        private readonly string? serializeName;
        private readonly string? deserializeName;
        const string defaultSerializeName = "Serialize";
        const string defaultDeserializeName = "Deserialize";
        public Names(string? serializeName, string? deserializeName)
        {
            this.serializeName = serializeName;
            this.deserializeName = deserializeName;
        }

        public static Names Combine(Names a, Names b)
        {
            return new Names(a.serializeName ?? b.serializeName, a.deserializeName ?? b.deserializeName);
        }

        public readonly string SerializeName => serializeName ?? defaultSerializeName;

        public readonly string DeserializeName => deserializeName ?? defaultDeserializeName;
    }
    private static readonly Dictionary<Assembly, Names> AssemblyNameMap = new();

    internal static T? FirstOrNull<T>(IEnumerable<T> enumerable, Func<T, bool> predicate)
        where T : struct
    {
        foreach (var v in enumerable)
        {
            if (predicate(v))
                return v;
        }
        return null;
    }
    private static Names GetAssemblyConfig(Assembly assembly)
    {
        var assemblyAttr = assembly.GetCustomAttributesData().FirstOrDefault(
            x => x.AttributeType.FullName == "NonSucking.Framework.Serialization.NoosonConfigurationAttribute");
        if (assemblyAttr is null)
            return new Names(null, null);
        var deserializeName = FirstOrNull(assemblyAttr.NamedArguments,
            argument => argument.MemberName == "NameOfStaticDeserializeWithCtor")?.TypedValue.Value?.ToString();
        var serializeName = FirstOrNull(assemblyAttr.NamedArguments,
            argument => argument.MemberName == "NameOfSerialize")?.TypedValue.Value?.ToString();
        return new Names(serializeName, deserializeName);
    }
    public static Names ResolveAssemblyConfig(Assembly assembly)
    {
#if NET6_0_OR_GREATER
        ref var names = ref CollectionsMarshal.GetValueRefOrAddDefault(AssemblyNameMap, assembly, out var exists);
        if (!exists)
        {
            names = GetAssemblyConfig(assembly);
        }
        return names;
#else
        if (!AssemblyNameMap.TryGetValue(assembly, out var names))
        {
            names = GetAssemblyConfig(assembly);
            AssemblyNameMap.Add(assembly, names);
        }
        return names;
#endif
    }
    
    
}
internal class TypeNameCache
{
    internal struct TypeConfig
    {
        public TypeConfig(bool isCustomMethodCall, Type serializeType, Type deserializeType, AssemblyNameCache.Names names)
        {
            IsCustomMethodCall = isCustomMethodCall;
            SerializeType = serializeType;
            DeserializeType = deserializeType;
            Names = names;
        }

        public bool IsCustomMethodCall { get; }
        public Type SerializeType { get; }
        public Type DeserializeType { get; }
        public AssemblyNameCache.Names Names { get; }
    }
    public static TypeConfig GetTypeConfig(Type type)
    {
        var names = AssemblyNameCache.ResolveAssemblyConfig(type.Assembly);
        // TODO: NoosonCustom attribute can be applied to properties
        var typeAttr = type.GetCustomAttributesData().FirstOrDefault(
            x => x.AttributeType.FullName == "NonSucking.Framework.Serialization.NoosonCustomAttribute");
        if (typeAttr is null)
            return new TypeConfig(false, type, type, names);
        var deserializeName = AssemblyNameCache.FirstOrNull(typeAttr.NamedArguments,
            argument => argument.MemberName == "DeserializeMethodName")?.TypedValue.Value?.ToString();
        var serializeName = AssemblyNameCache.FirstOrNull(typeAttr.NamedArguments,
            argument => argument.MemberName == "SerializeMethodName")?.TypedValue.Value?.ToString();
        names = AssemblyNameCache.Names.Combine(names, new AssemblyNameCache.Names(serializeName, deserializeName));

        var serializeType = AssemblyNameCache.FirstOrNull(typeAttr.NamedArguments,
            argument => argument.MemberName == "SerializeImplementationType")?.TypedValue.Value as Type ?? type;
        var deserializeType = AssemblyNameCache.FirstOrNull(typeAttr.NamedArguments,
            argument => argument.MemberName == "DeserializeImplementationType")?.TypedValue.Value as Type ?? type;
        
        return new TypeConfig(true, serializeType, deserializeType, names);
    }
}

internal class MethodResolver
{
    private static Type[] MatchGenericArguments(MethodInfo x, ParameterInfo[] parameters, Type[] parameterTypes)
    {
        // TODO: cannot match generic parameters that are not used as parameter types
        var genArguments = x.GetGenericArguments();
        var matchedParameters = new Type[genArguments.Length];
        for (var index = 0; index < genArguments.Length; index++)
        {
            var genArg = genArguments[index];
            for (var pIndex = 0; pIndex < parameters.Length; pIndex++)
            {
                var genParam = parameters[pIndex];
                if (genParam.ParameterType != genArg)
                    continue;
                var previouslyMatched = matchedParameters[index];
                var newlyMatched = parameterTypes[pIndex];
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (previouslyMatched is null || newlyMatched.IsAssignableFrom(previouslyMatched))
                {
                    matchedParameters[index] = newlyMatched;
                }
            }
        }

        return matchedParameters;
    }
    internal static MethodInfo? GetBestMatch(Type type, string methodName, BindingFlags bindingFlags, Type[] parameterTypes, Type? returnType)
    {
        var methods = type.GetMethods(bindingFlags)
            .Where(x => x.Name == methodName
                   && x.ReturnType == (returnType ?? typeof(void)))
            .Select(x =>
                 {
                     try
                     {
                         var parameters = x.GetParameters();
                         if (parameters.Length != parameterTypes.Length)
                             return null;
                         if (x.IsGenericMethodDefinition)
                         {
                             var matched = MatchGenericArguments(x, parameters, parameterTypes);
                             return x.MakeGenericMethod(matched);
                         }
                         return x;
                     }
                     catch (ArgumentException)
                     {
                         return null;
                     }
                 }).Where(x => x is not null).OfType<MethodInfo>().ToArray();

        var perfectMatch = methods.FirstOrDefault(x =>
                                                  {
                                                      if (x.IsGenericMethod)
                                                          return false;
                                                      var parameters = x.GetParameters();
                                                      for (int i = 0; i < parameters.Length; i++)
                                                      {
                                                          if (parameters[i].ParameterType != parameterTypes[i])
                                                          {
                                                              return false;
                                                          }
                                                      }

                                                      return true;
                                                  });
        if (perfectMatch is not null)
            return perfectMatch;
        if (methods.Length == 0)
            return null;
        // ReSharper disable once CoVariantArrayConversion
        return Type.DefaultBinder.SelectMethod(bindingFlags, methods, parameterTypes, null) as MethodInfo;
    }
}

internal class SerializerInformationBase<TTypeIdentifier>
{
    public SerializerInformationBase(TTypeIdentifier identifier, Type type)
    {
        Identifier = identifier;
        Type = type;
    }
    public TTypeIdentifier Identifier { get; }
    public Type Type { get; }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Identifier is null ? 0 : (EqualityComparer<TTypeIdentifier>.Default.GetHashCode(Identifier) * 397)) ^ Type.GetHashCode();
        }
    }
}

internal class
    SerializerInformation<TWriter, TTypeIdentifier> : SerializerInformationBase<TTypeIdentifier>, IEquatable<
        SerializerInformation<TWriter, TTypeIdentifier>>
{
    public SerializerInformation(TTypeIdentifier identifier, Type type)
        : base(identifier, type)
    {
#if __NOOSON_ADVANCED_SERIALIZATION__
        Serialize = NonSucking.Framework.Serialization.Advanced.SerializeGenerating<TWriter>.GenerateSerializeDelegate(type, false);
#else
        var typeConfig = TypeNameCache.GetTypeConfig(type);
        var useStaticSerialize = typeConfig.SerializeType != type;
        var serializerMethod = MethodResolver.GetBestMatch(typeConfig.SerializeType, typeConfig.Names.SerializeName,
            BindingFlags.Public | BindingFlags.NonPublic |
            (useStaticSerialize ? BindingFlags.Static : BindingFlags.Instance),
            useStaticSerialize ? new[] { typeof(TWriter), type } : new[] { typeof(TWriter) }, null);

        if (serializerMethod is not null)
        {
            var valueParam = Expression.Parameter(type);
            var writerParam = Expression.Parameter(typeof(TWriter));
            var methodCall = useStaticSerialize
                ? Expression.Call(null, serializerMethod, writerParam, valueParam)
                : Expression.Call(valueParam, serializerMethod, writerParam);
            Serialize = Expression.Lambda(methodCall, writerParam, valueParam).Compile();
        }
        else
        {
            throw new NotSupportedException(
                $"Serialization of {type.Name} is not supported. More complex types are only supported using \"NonSucking.Framework.Serialization.Advanced\"");
        }
#endif
    }
    public Delegate? Serialize { get; }

    public bool Equals(SerializerInformation<TWriter, TTypeIdentifier>? other)
    {
        if (other is null) return false;
        return Type == other.Type;
    }

    public override bool Equals(object? other)
    {
        return other is SerializerInformation<TWriter, TTypeIdentifier> serializerInformation
               && Equals(serializerInformation);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ typeof(TWriter).GetHashCode();
        }
    }
}

internal class
    DeserializerInformation<TReader, TTypeIdentifier> : SerializerInformationBase<TTypeIdentifier>, IEquatable<
        DeserializerInformation<TReader, TTypeIdentifier>>
{
    public DeserializerInformation(TTypeIdentifier identifier, Type type)
        : base(identifier, type)
    {
#if __NOOSON_ADVANCED_SERIALIZATION__
        Deserialize = NonSucking.Framework.Serialization.Advanced.DeserializeGenerating<TReader>.GenerateDeserializeDelegate(type, false);
#else
        var typeConfig = TypeNameCache.GetTypeConfig(type);
        var useStaticSerialize = typeConfig.SerializeType != type;
        var deserializerMethod = MethodResolver.GetBestMatch(typeConfig.DeserializeType,
            typeConfig.Names.DeserializeName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            new[] { typeof(TReader) }, type);


        if (deserializerMethod is not null)
        {
            var readerParam = Expression.Parameter(typeof(TReader));

            Deserialize = Expression.Lambda(Expression.Call(null, deserializerMethod, readerParam), readerParam)
                .Compile();
        }
        else
        {
            throw new NotSupportedException(
                $"Deserialization of {type.Name} is not supported. More complex types are only supported using \"NonSucking.Framework.Serialization.Advanced\"");
        }
#endif
    }
    public Delegate? Deserialize { get; }

    public bool Equals(DeserializerInformation<TReader, TTypeIdentifier>? other)
    {
        if (other is null) return false;
        return Type == other.Type;
    }

    public override bool Equals(object? other)
    {
        return other is DeserializerInformation<TReader, TTypeIdentifier> serializerInformation
               && Equals(serializerInformation);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ typeof(TReader).GetHashCode();
        }
    }
}
