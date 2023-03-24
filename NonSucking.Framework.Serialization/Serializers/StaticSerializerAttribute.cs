using System;

namespace NonSucking.Framework.Serialization.Serializers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class StaticSerializerAttribute : Attribute
{
    public StaticSerializerAttribute(int priority = 0)
    {
        SerializerPriority = priority;
    }
    public StaticSerializerAttribute(int serializerPriority, int deserializerPriority)
    {
        SerializerPriority = serializerPriority;
        DeserializerPriority = deserializerPriority;
    }

    public int SerializerPriority { get; }
    public int DeserializerPriority { get; }
}