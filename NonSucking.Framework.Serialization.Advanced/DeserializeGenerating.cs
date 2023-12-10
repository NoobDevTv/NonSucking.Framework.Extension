using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NonSucking.Framework.Serialization.Advanced;

public static class DeserializeGenerating<TReader>
{
    public static MethodInfo GenerateDeserialize(FieldInfo prop)
    {
        return GenerateDeserialize(prop.FieldType, NullabilityHelper.Context.Create(prop));
    }
    
    public static MethodInfo GenerateDeserialize(PropertyInfo prop)
    {
        return GenerateDeserialize(prop.PropertyType, NullabilityHelper.Context.Create(prop));
    }
    

    public static MethodInfo GenerateDeserialize(Type type, bool isNullable)
    {
        return GenerateDeserialize(type, NullabilityHelper.CreateNullable(type, isNullable));
    }

    private static MethodInfo GenerateDeserialize(Type type, NullabilityInfo nullability)
    {
        var gen = new DeserializeGenerator(typeof(TReader), TypeNameCache.GetTypeConfig(type),
            new BaseGeneratorContext.TypeContext(type, nullability),
            BaseGeneratorContext.ContextType.Deserialize);
        var generatedContext = gen.CurrentContext;
        
        var generatedType = generatedContext.Build();   

        return generatedType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static)!;
    }
    public static Delegate GenerateDeserializeDelegate(Type type, bool isNullable)
    {
        var m = GenerateDeserialize(type, isNullable);
        var reader = Expression.Parameter(typeof(TReader), "reader");
        return Expression.Lambda(Expression.Call(m, reader), reader).Compile();
    }
}