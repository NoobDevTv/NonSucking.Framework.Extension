using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NonSucking.Framework.Serialization;
[assembly: NoosonConfiguration(
    GenerateDeserializeExtension = false, 
    DisableWarnings = true, 
    GenerateStaticDeserializeWithCtor = true,
    GenerateDeserializeOnInstance = true,
    GenerateStaticSerialize = true,
    GenerateStaticDeserializeIntoInstance =true,
    NameOfStaticDeserializeWithCtor = "WithCtorBase",
    NameOfDeserializeOnInstance = "OnInstanceBase",
    NameOfStaticDeserializeIntoInstance = "IntoInstanceBase",
    NameOfStaticDeserializeWithOutParams = "ThisHasOutParams2",
    NameOfSerialize = "OwnSerialize",
    NameOfStaticSerialize = "StaticSerialize")]