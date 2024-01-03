using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class RecursiveCallSerializer
{
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var serializerMethod = generator.CurrentContext.Method;
        if (!generator.ContextMap.TryGetValue(generator.CurrentContext.ValueType, out var context) || context == generator.CurrentContext || !context.IsTopLevel)
            return false;
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.CurrentContext.GetValue!(generator.Il);
        generator.Il.Emit(OpCodes.Call, serializerMethod);
        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var deserializerMethod = generator.CurrentContext.Method;
        if (!generator.ContextMap.TryGetValue(generator.CurrentContext.ValueType, out var context) || context == generator.CurrentContext || !context.IsTopLevel)
            return false;
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Call, deserializerMethod);
        generator.CurrentContext.SetValue!(generator.Il);
        return true;
    }
}