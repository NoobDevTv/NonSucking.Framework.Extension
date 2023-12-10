using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace NonSucking.Framework.Serialization.Advanced;

public abstract class BaseGeneratorContext
{
    public enum ContextType
    {
        Serialize,
        Deserialize
    }
    public readonly TypeBuilder? typeBuilder;
    public class TypeContext : IEquatable<TypeContext>
    {
        public TypeContext(Type type, NullabilityInfo nullabilityInfo)
        {
            Type = type;
            NullabilityInfo = nullabilityInfo;
        }

        public Type Type { get; }
        public NullabilityInfo NullabilityInfo { get; }

        public bool Equals(TypeContext? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && Equals(NullabilityInfo, other.NullabilityInfo);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypeContext)obj);
        }

        private static bool Equals(NullabilityInfo a, NullabilityInfo b)
        {
            if (a.Type != b.Type ||
                a.ReadState != b.ReadState ||
                a.WriteState != b.WriteState)
                return false;

            if (a.ElementType is not null && b.ElementType is not null)
            {
                if (!Equals(a.ElementType, b.ElementType))
                    return false;
            }
            else if(a.ElementType != b.ElementType)
            {
                return false;
            }

            if (a.GenericTypeArguments.Length != b.GenericTypeArguments.Length)
                return false;

            for (int i = 0; i < a.GenericTypeArguments.Length; i++)
            {
                if (!Equals(a.GenericTypeArguments[i], b.GenericTypeArguments[i]))
                    return false;
            }

            return true;
        }

        private static int GetHashCode(NullabilityInfo a)
        {
            return HashCode.Combine(a.Type.GetHashCode(), a.ReadState.GetHashCode(), a.WriteState.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type.GetHashCode(), GetHashCode(NullabilityInfo));
        }
    }
    public BaseGeneratorContext(TypeContext valueType, ContextType contextType,
        Action<ILGenerator> getReaderWriter, bool isTopLevel, MethodBuilder method)
    {
        if (contextType != ContextType.Serialize && contextType != ContextType.Deserialize)
            throw new ArgumentOutOfRangeException(nameof(contextType));
        ValueType = valueType;
        Type = contextType;
        GetReaderWriter = getReaderWriter;
        IsTopLevel = isTopLevel;
        Method = method;
        Il = Method.GetILGenerator();
    }
    public BaseGeneratorContext(Type readerWriterType, TypeContext valueType, ContextType contextType, Action<ILGenerator> getReaderWriter, bool isTopLevel)
    {
        if (contextType != ContextType.Serialize && contextType != ContextType.Deserialize)
            throw new ArgumentOutOfRangeException(nameof(contextType));

        ValueType = valueType;
        Type = contextType;
        GetReaderWriter = getReaderWriter;
        IsTopLevel = isTopLevel;
        var methodName = contextType == ContextType.Serialize ? "Serialize" : "Deserialize";
        var retType = contextType == ContextType.Serialize ? null : valueType.Type;
        var paramTypes = contextType == ContextType.Serialize
            ? new[] { readerWriterType, valueType.Type }
            : new[] { readerWriterType };

        var assemblyName = new AssemblyName($"{valueType.Type.ToAssemblyNameString()}{readerWriterType}");
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(assemblyName,
                AssemblyBuilderAccess.Run);
        // Add IgnoresAccessChecksTo attribute

        var ignoreAccessChecksToCtor = typeof(IgnoresAccessChecksToAttribute).GetConstructor(new[] { typeof(string) })
                                       ?? throw new InvalidProgramException(
                                           $"{nameof(IgnoresAccessChecksToAttribute)} constructor with string argument is missing!");

        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name
            ?? throw new InvalidOperationException("Could not resolve entry assembly name");
        var callingAssemblyName = Assembly.GetCallingAssembly().GetName().Name
            ?? throw new InvalidOperationException("Could not resolve calling assembly name");
        
        var ignoresAccessChecksTo = new CustomAttributeBuilder
        (
            ignoreAccessChecksToCtor,
            new object[] { entryAssemblyName }
        );

        assemblyBuilder.SetCustomAttribute(ignoresAccessChecksTo);
        ignoresAccessChecksTo = new CustomAttributeBuilder
        (
            ignoreAccessChecksToCtor,
            new object[] { callingAssemblyName }
        );

        assemblyBuilder.SetCustomAttribute(ignoresAccessChecksTo);
        var mod = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilderLocal = mod.DefineType("SerializeStuff");
        typeBuilder = typeBuilderLocal;
        Method = typeBuilderLocal.DefineMethod(methodName,
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            retType, paramTypes);
        Il = Method.GetILGenerator();
        AssemblyBuilder = assemblyBuilder;
    }
public AssemblyBuilder AssemblyBuilder { get; }
    public Type Build()
    {
        if (typeBuilder is null)
            throw new InvalidOperationException("Cannot Build type for already existent method");
        return typeBuilder.CreateType();
    }
    
    public Action<ILGenerator> GetReaderWriter { get; }
    public Action<ILGenerator>? GetValue { get; set; }
    public Action<ILGenerator>? GetValueRef { get; set; }
    public Action<ILGenerator>? SetValue { get; set; }
    
    public ILGenerator Il { get; }

    public MethodBuilder Method { get; }

    public TypeContext ValueType { get; }
    public ContextType Type { get; }
    public bool IsTopLevel { get; }

    public abstract BaseGeneratorContext SubContext(TypeContext valueType, bool? isTopLevel);
}