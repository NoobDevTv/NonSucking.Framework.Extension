namespace NonSucking.Framework.Serialization.SerializerCollector;

public struct StaticSerializerInfo
{
    public StaticSerializerInfo(int serializerPriority, int deserializerPriority, bool isFinalizer)
    {
        SerializerPriority = serializerPriority;
        DeserializerPriority = deserializerPriority;
        IsFinalizer = isFinalizer;
    }

    public int SerializerPriority { get; }
    public int DeserializerPriority { get; }
    public bool IsFinalizer { get;  }
}