using System;

namespace NonSucking.Framework.Serialization.Serializers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class StaticSerializerAttribute : Attribute
{
    public StaticSerializerAttribute(int priority = 0)
    {
        Priority = priority;
    }

    public int Priority { get; }
}