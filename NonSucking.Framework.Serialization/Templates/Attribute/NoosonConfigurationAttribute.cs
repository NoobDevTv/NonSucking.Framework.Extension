#nullable enable
using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    internal class NoosonConfigurationAttribute : System.Attribute
    {

        public bool GenerateDeserializeExtension { get; set; } = true;
        //public bool EnableNullability { get; set; } = true; Not supported yet, as this requires alot of work to undo 
        public bool DisableWarnings { get; set; } = false;

        public bool GenerateStaticDeserializeWithCtor { get; set; } = true;
        public string NameOfStaticDeserializeWithCtor { get; set; } = "Deserialize";

        public string NameOfStaticDeserializeWithOutParams { get; set; } = "Deserialize";

        /// <summary>
        /// Will be automatically enabled when <see cref="GenerateDeserializeOnInstance"/> or <see cref="GenerateDeserializeExtension"/> is enabled
        /// </summary>
        public bool GenerateStaticDeserializeIntoInstance { get; set; } = true; //Currently required for GenerateDeserializeOnInstance 
        public string NameOfStaticDeserializeIntoInstance { get; set; } = "Deserialize";

        public bool GenerateDeserializeOnInstance { get; set; } = true;
        public string NameOfDeserializeOnInstance { get; set; } = "DeserializeSelf";

        public bool GenerateStaticSerialize { get; set; } = true;
        public string NameOfStaticSerialize { get; set; } = "Serialize";
        public string NameOfSerialize { get; set; } = "Serialize";

    }
}


