using System;

namespace NonSucking.Framework.Extension.Serialization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class NoosonCustomAttribute : Attribute
    {
        public Type SerializeImplementationType { get; }
        public Type DeserializeImplmentationType { get; }
        public string SerializeMethodName { get;  }
        public string DeserializeMethodName { get;  }
        public bool StaticOnly { get; }

        private const string deserializeName = "Deserialize";
        private const string serializeName = "Serialize";

        /// <summary>
        /// Calls a static method inside the given type
        /// </summary>
        /// <param name="serializeImplementationType">Type of class which holds the method</param>
        /// <param name="methodName">nameof the method to call</param>
        public NoosonCustomAttribute(Type serializeImplementationType = default, string serializeMethodName = serializeName, Type deserializeImplementationType = default, string deserializeMethodName = deserializeName)
        {
            SerializeImplementationType = serializeImplementationType;
            DeserializeImplmentationType = deserializeImplementationType;
            SerializeMethodName = serializeMethodName;
            DeserializeMethodName = deserializeMethodName;
            StaticOnly = true;
        }
        /// <summary>
        /// Calls a static method inside the given type
        /// </summary>
        /// <param name="methodImplementationType">Type of class which holds the method</param>
        /// <param name="methodName">nameof the method to call</param>
        public NoosonCustomAttribute(Type methodImplementationType, string serializeMethodName = serializeName, string deserializeMethodName = deserializeName)
        {
            SerializeImplementationType = methodImplementationType;
            DeserializeImplmentationType = methodImplementationType;
            SerializeMethodName = serializeMethodName;
            DeserializeMethodName = deserializeMethodName;
            StaticOnly = true;
        }

        /// <summary>
        /// Calls a static Serialize method inside the given type
        /// </summary>
        /// <param name="methodImplementationType">Type of class which holds the method</param>
        public NoosonCustomAttribute(Type methodImplementationType)
        {
            SerializeImplementationType = methodImplementationType;
            DeserializeImplmentationType = methodImplementationType;
            SerializeMethodName = serializeName;
            SerializeMethodName = deserializeName;
            StaticOnly = true;
        }

        /// <summary>
        /// Calls a method inside this type
        /// </summary>
        /// <param name="methodName">nameof the method to call</param>
        public NoosonCustomAttribute(string serializeMethodName = serializeName, string deserializeMethodName = deserializeName)
        {
            SerializeMethodName = serializeMethodName;
            DeserializeMethodName = deserializeMethodName;
            StaticOnly = false;
        }
        

        /// <summary>
        /// Calls a static Serialize method inside the given type
        /// </summary>
        public NoosonCustomAttribute()
        {
            SerializeMethodName = serializeName;
            SerializeMethodName = deserializeName;
            StaticOnly = false;
        }

    }
}