using System;

namespace NonSucking.Framework.Serialization;

public class TypeSerializerException : Exception
{
    public TypeSerializerException(string message)
        : base(message)
    {
        
    }
}