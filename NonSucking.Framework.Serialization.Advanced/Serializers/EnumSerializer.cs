using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class EnumSerializer
{
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;
        if (!type.IsEnum)
            return false;
        var serializerMethod = Helper.TryGetWrite(generator, type.GetEnumUnderlyingType());
        if (serializerMethod is null)
            return false;

        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.CurrentContext.GetValue!(generator.Il);
        
        generator.Il.Emit(OpCodes.Callvirt, serializerMethod);
        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;
        if (!type.IsEnum)
            return false;
        var underlyingType = type.GetEnumUnderlyingType();
        var deserializerMethod = Helper.TryGetRead(generator, underlyingType);
        if (deserializerMethod is null)
        {
            return false;
        }

        
        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Call, deserializerMethod);

        generator.CurrentContext.SetValue!(generator.Il);

        return true;
    }
}