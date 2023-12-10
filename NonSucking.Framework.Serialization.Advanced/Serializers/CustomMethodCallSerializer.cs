namespace NonSucking.Framework.Serialization.Advanced;

internal class CustomMethodCallSerializer
{
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        if (!typeConfig.IsCustomMethodCall)
            return false;
        
        return MethodCallSerializer.Serialize(generator, typeConfig);
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        if (!typeConfig.IsCustomMethodCall)
            return false;
        return MethodCallSerializer.Deserialize(generator, typeConfig);
    }
}