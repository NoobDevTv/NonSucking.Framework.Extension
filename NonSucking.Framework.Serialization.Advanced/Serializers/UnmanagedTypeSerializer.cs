namespace NonSucking.Framework.Serialization.Advanced;

internal class UnmanagedTypeSerializer
{

    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;
        if (!type.IsUnmanaged())
            return false;

        var serializerMethod = Helper.GetWrite(generator, type, "WriteUnmanaged");

        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.CurrentContext.GetValue!(generator.Il);
        
        generator.EmitCall(serializerMethod);

        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;
        if (!type.IsUnmanaged())
            return false;
        

        var deserializerMethod = Helper.GetRead(generator, type, "ReadUnmanaged");

        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.EmitCall(deserializerMethod);
        generator.CurrentContext.SetValue!(generator.Il);


        return true;
    }
}