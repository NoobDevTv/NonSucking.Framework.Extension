using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NonSucking.Framework.Serialization.Advanced;

public static class NullabilityHelper
{
    public delegate NullabilityInfo NullabilityInfoCreationDelegate(
        Type type,
        NullabilityState readState,
        NullabilityState writeState,
        NullabilityInfo? elementType,
        NullabilityInfo[] typeArguments);

    public static readonly NullabilityInfoCreationDelegate CreateNullability;

    static NullabilityHelper()
    {
        var ctorParamTypes = new[]
                             {
                                 typeof(Type),
                                 typeof(NullabilityState),
                                 typeof(NullabilityState),
                                 typeof(NullabilityInfo),
                                 typeof(NullabilityInfo[])
                             };
        var ctor = typeof(NullabilityInfo).GetConstructor(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
            ctorParamTypes) ?? throw new InvalidProgramException($"Could not find constructor for '{nameof(NullabilityInfo)}'");

        var ctorParams = ctorParamTypes.Select(Expression.Parameter).ToArray();
            
        CreateNullability = Expression.Lambda<NullabilityInfoCreationDelegate>(Expression.New(ctor, ctorParams), ctorParams).Compile();
    }
    public static readonly NullabilityInfoContext Context = new();
    internal static NullabilityInfo CreateNullable(Type type, bool isNullable)
    {
        if (type.IsGenericType && type.IsAssignableToOpenGeneric(typeof(Nullable<>)))
            isNullable = true;
        
        var elementType = type.IsSZArray ? CreateNullable(type.GetElementType()!, false) : null;
        var typeArguments = type.IsGenericType
            ? type.GenericTypeArguments.Select(x => CreateNullable(x, false)).ToArray()
            : Array.Empty<NullabilityInfo>();
        
        return CreateNullability(
            type,
            isNullable ? NullabilityState.Nullable : NullabilityState.NotNull,
            isNullable ? NullabilityState.Nullable : NullabilityState.NotNull,
            elementType,
            typeArguments);
    }
}