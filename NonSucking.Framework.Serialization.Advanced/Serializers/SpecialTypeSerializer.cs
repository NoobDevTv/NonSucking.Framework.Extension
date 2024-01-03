using System;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class SpecialTypeSerializer
{
    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;
        var typeCode = Type.GetTypeCode(type);
        if (typeCode != TypeCode.String && typeCode is < TypeCode.Boolean or > TypeCode.Double)
            return false;
        var serializerMethod = Helper.TryGetWrite(generator, type);
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
        var typeCode = Type.GetTypeCode(type);
        if (typeCode != TypeCode.String && typeCode is < TypeCode.Boolean or > TypeCode.Double)
            return false;
        var deserializerMethod = Helper.TryGetRead(generator, type, typeCode);
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