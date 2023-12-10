using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NonSucking.Framework.Serialization.Advanced;

public static class SerializeGenerating<TWriter>
{
    public static MethodInfo GenerateSerialize(FieldInfo prop)
    {
        return GenerateSerialize(prop.FieldType, NullabilityHelper.Context.Create(prop));
    }
    public static MethodInfo GenerateSerialize(PropertyInfo prop)
    {
        return GenerateSerialize(prop.PropertyType, NullabilityHelper.Context.Create(prop));
    }
    
    public static MethodInfo GenerateSerialize(Type type, bool isNullable)
    {
        return GenerateSerialize(type, NullabilityHelper.CreateNullable(type, isNullable));
    }
    
    private static MethodInfo GenerateSerialize(Type type, NullabilityInfo nullability)
    {
        var gen = new SerializeGenerator(typeof(TWriter), TypeNameCache.GetTypeConfig(type),
            new BaseGeneratorContext.TypeContext(type, nullability));
        return gen.CurrentContext.Build().GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static)!;
    }

    public static Delegate GenerateSerializeDelegate(Type type, bool isNullable)
    {
        var m = GenerateSerialize(type, isNullable);
        var writer = Expression.Parameter(typeof(TWriter), "writer");
        var value = Expression.Parameter(type, "value");
        return Expression.Lambda(Expression.Call(m, writer, value), writer, value).Compile();
    }
}