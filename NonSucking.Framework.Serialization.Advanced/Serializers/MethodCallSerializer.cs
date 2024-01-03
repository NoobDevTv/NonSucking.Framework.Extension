using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class MethodCallSerializer
{
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var useStaticSerialize = typeConfig.SerializeType != generator.CurrentContext.ValueType.Type;
        var serializerMethod = MethodResolver.GetBestMatch(typeConfig.SerializeType, typeConfig.Names.SerializeName,
            BindingFlags.Public | BindingFlags.NonPublic |
            (useStaticSerialize ? BindingFlags.Static : BindingFlags.Instance),
            useStaticSerialize ? new[] { generator.CurrentContext.WriterType, generator.CurrentContext.ValueType.Type } : new[] { generator.CurrentContext.WriterType }, null);
        if (serializerMethod is null)
            return false;
        if (serializerMethod.IsStatic)
        {
            generator.CurrentContext.GetReaderWriter(generator.Il);
            generator.CurrentContext.GetValue!(generator.Il);
        }
        else
        {
            generator.CurrentContext.GetValue!(generator.Il);
            generator.CurrentContext.GetReaderWriter(generator.Il);
        }
        generator.EmitCall(serializerMethod);
        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var deserializerMethod = MethodResolver.GetBestMatch(typeConfig.DeserializeType,
            typeConfig.Names.DeserializeName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            new[] { generator.CurrentContext.ReaderType }, generator.CurrentContext.ValueType.Type);
        if (deserializerMethod is null)
        {
            return false;
        }

        
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Call, deserializerMethod);

        generator.CurrentContext.SetValue!(generator.Il);
        // generator.CurrentContext.GetValue = ilGenerator => ilGenerator.Emit(OpCodes.Ldloc, serializerVar);
        // generator.CurrentContext.SetValue = ilGenerator => ilGenerator.Emit(OpCodes.Stloc, serializerVar);

        return true;
    }
}