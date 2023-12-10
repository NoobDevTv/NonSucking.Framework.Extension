using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NonSucking.Framework.Serialization.Advanced;

internal class AssemblyNameCache
{
    internal readonly struct Names
    {
        private readonly string? serializeName;
        private readonly string? deserializeName;
        private readonly string? deserializeOutName;
        private const string DefaultSerializeName = "Serialize";
        private const string DefaultDeserializeName = "Deserialize";
        public Names(string? serializeName, string? deserializeName, string? deserializeOutName)
        {
            this.serializeName = serializeName;
            this.deserializeName = deserializeName;
            this.deserializeOutName = deserializeOutName;
        }

        public static Names Combine(Names a, Names b)
        {
            return new Names(a.serializeName ?? b.serializeName, a.deserializeName ?? b.deserializeName, a.deserializeOutName ?? b.deserializeOutName);
        }

        public string SerializeName => serializeName ?? DefaultSerializeName;

        public string DeserializeName => deserializeName ?? DefaultDeserializeName;
        public string DeserializeOutName => deserializeOutName ?? DefaultDeserializeName;
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
            x => x.AttributeType.FullName == Consts.NoosonConfigurationAttribute);
        if (assemblyAttr is null)
            return new Names(null, null, null);
        var deserializeName = FirstOrNull(assemblyAttr.NamedArguments,
            argument => argument.MemberName == "NameOfStaticDeserializeWithCtor")?.TypedValue.Value?.ToString();
        var deserializeOutName = FirstOrNull(assemblyAttr.NamedArguments,
            argument => argument.MemberName == "NameOfStaticDeserializeWithOutParams")?.TypedValue.Value?.ToString();
        var serializeName = FirstOrNull(assemblyAttr.NamedArguments,
            argument => argument.MemberName == "NameOfSerialize")?.TypedValue.Value?.ToString();
        return new Names(serializeName, deserializeName, deserializeOutName);
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