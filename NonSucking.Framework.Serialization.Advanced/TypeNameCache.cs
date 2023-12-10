using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NonSucking.Framework.Serialization.Advanced;

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

        public override string ToString()
        {
            return $"{SerializeType.Name}.{Names.SerializeName}/{DeserializeType.Name}.{Names.DeserializeName}";
        }
    }

    internal static CustomAttributeData? GetFirstNoosonCustom(IList<CustomAttributeData> data)
    {
        return data.FirstOrDefault(
            x => x.AttributeType.FullName == Consts.NoosonCustomAttribute);
    }

    private static TypeConfig GetTypeConfig(CustomAttributeData attr, AssemblyNameCache.Names names, Type type)
    {
        var deserializeName = AssemblyNameCache.FirstOrNull(attr.NamedArguments,
            argument => argument.MemberName == "DeserializeMethodName")?.TypedValue.Value?.ToString();
        var serializeName = AssemblyNameCache.FirstOrNull(attr.NamedArguments,
            argument => argument.MemberName == "SerializeMethodName")?.TypedValue.Value?.ToString();
        names = AssemblyNameCache.Names.Combine(names, new AssemblyNameCache.Names(serializeName, deserializeName, null));

        var serializeType = AssemblyNameCache.FirstOrNull(attr.NamedArguments,
            argument => argument.MemberName == "SerializeImplementationType")?.TypedValue.Value as Type ?? type;
        var deserializeType = AssemblyNameCache.FirstOrNull(attr.NamedArguments,
            argument => argument.MemberName == "DeserializeImplementationType")?.TypedValue.Value as Type ?? type;
        
        return new TypeConfig(true, serializeType, deserializeType, names);
    }
    
    private static TypeConfig GetTypeConfig(Type type, AssemblyNameCache.Names names)
    {
        var typeAttr = GetFirstNoosonCustom(type.GetCustomAttributesData());
        if (typeAttr is null)
            return new TypeConfig(false, type, type, names);
        return GetTypeConfig(typeAttr, names, type);
    }
    public static TypeConfig GetTypeConfig(Type type)
    {
        var names = AssemblyNameCache.ResolveAssemblyConfig(type.Assembly);
        return GetTypeConfig(type, names);
    }

    public static TypeConfig GetTypeConfig(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        var names = AssemblyNameCache.ResolveAssemblyConfig(type.Assembly);
        var propAttr = GetFirstNoosonCustom(prop.GetCustomAttributesData());
        if (propAttr is null)
            return GetTypeConfig(type, names);
        return GetTypeConfig(propAttr, names, type);
    }

    public static TypeConfig GetTypeConfig(FieldInfo prop)
    {
        var type = prop.FieldType;
        var names = AssemblyNameCache.ResolveAssemblyConfig(type.Assembly);
        var propAttr = GetFirstNoosonCustom(prop.GetCustomAttributesData());
        if (propAttr is null)
            return GetTypeConfig(type, names);
        return GetTypeConfig(propAttr, names, type);
    }
    
}
