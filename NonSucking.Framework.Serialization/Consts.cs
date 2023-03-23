using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Serialization;
internal static class Consts
{
    internal const string GenericParameterReaderName = "TNonSuckingReader";
    internal const string GenericParameterWriterName = "TNonSuckingWriter";
    internal const string GenericParameterReaderInterface = "IBinaryReader";
    internal const string GenericParameterWriterInterface = "IBinaryWriter";
    internal const string GenericParameterReaderInterfaceFull = $"{NoosonNamespace}.{GenericParameterReaderInterface}";
    internal const string GenericParameterWriterInterfaceFull = $"{NoosonNamespace}.{GenericParameterWriterInterface}";
    internal const string Serialize = nameof(Serialize);
    internal const string Deserialize = nameof(Deserialize);
    internal const string DeserializeSelf = nameof(DeserializeSelf);
    internal const string InstanceParameterName = "that";
    internal const string NoosonNamespace = "NonSucking.Framework.Serialization";
    internal const string ThisName = "this";
    internal const string LocalVariableSuffix = "_️"; //VARIATION SELECTOR-16 _, looks better and provides better uniqueness
}
