using Microsoft.CodeAnalysis;

using System.Linq;

namespace NonSucking.Framework.Serialization
{
    public record struct NoosonConfig(
        bool GenerateDeserializeExtension = true,
        bool DisableWarnings = false,
        string NameOfStaticDeserializeWithOutParams = Consts.Deserialize,
        
        bool GenerateStaticDeserializeWithCtor = true,
        string NameOfStaticDeserializeWithCtor = Consts.Deserialize,
        
        bool GenerateStaticDeserializeIntoInstance = true,
        string NameOfStaticDeserializeIntoInstance = Consts.Deserialize,

        bool GenerateDeserializeOnInstance = true,
        string NameOfDeserializeOnInstance = Consts.DeserializeSelf,

        bool GenerateStaticSerialize = true,
        string NameOfStaticSerialize = Consts.Serialize,
        string NameOfSerialize = Consts.Serialize,

        bool EnableNullability = false)
    {
        public int ShouldContainMethodsCount { get; private set; }

        public NoosonConfig ReloadFrom(AttributeData? data)
        {
            if (data is null)
                return this;
            var nm = data.NamedArguments;
            var config = this;
            if (nm.FirstOrDefault(x => x.Key == nameof(EnableNullability)).Value.Value is bool disableNullable)
                config = config with { EnableNullability = disableNullable };

            if (nm.FirstOrDefault(x => x.Key == nameof(DisableWarnings)).Value.Value is bool disableWarnings)
                config = config with { DisableWarnings = disableWarnings };

            if (nm.FirstOrDefault(x => x.Key == nameof(NameOfStaticDeserializeWithOutParams)).Value.Value is string nameOfStaticDeserializeWithOutParams)
                config = config with { NameOfStaticDeserializeWithOutParams = nameOfStaticDeserializeWithOutParams };
            
            if (nm.FirstOrDefault(x => x.Key == nameof(GenerateDeserializeExtension)).Value.Value is bool generateExtensions)
                config = config with { GenerateDeserializeExtension = generateExtensions };

            if (nm.FirstOrDefault(x => x.Key == nameof(GenerateStaticDeserializeWithCtor)).Value.Value is bool generateStaticDeserializeWithCtor)
                config = config with { GenerateStaticDeserializeWithCtor = generateStaticDeserializeWithCtor };
            if (nm.FirstOrDefault(x => x.Key == nameof(NameOfStaticDeserializeWithCtor)).Value.Value is string nameOfStaticDeserializeWithCtor)
                config = config with { NameOfStaticDeserializeWithCtor = nameOfStaticDeserializeWithCtor };
            
            if (nm.FirstOrDefault(x => x.Key == nameof(GenerateDeserializeOnInstance)).Value.Value is bool generateDeserializeOnInstance)
                config = config with { GenerateDeserializeOnInstance = generateDeserializeOnInstance };
            if (nm.FirstOrDefault(x => x.Key == nameof(NameOfDeserializeOnInstance)).Value.Value is string nameOfDeserializeOnInstance)
                config = config with { NameOfDeserializeOnInstance = nameOfDeserializeOnInstance };            

            if (nm.FirstOrDefault(x => x.Key == nameof(GenerateStaticDeserializeIntoInstance)).Value.Value is bool generateStaticDeserializeIntoInstance)
                config = config with { GenerateStaticDeserializeIntoInstance = generateStaticDeserializeIntoInstance };
            if (nm.FirstOrDefault(x => x.Key == nameof(NameOfStaticDeserializeIntoInstance)).Value.Value is string nameOfStaticDeserializeIntoInstance)
                config = config with { NameOfStaticDeserializeIntoInstance = nameOfStaticDeserializeIntoInstance };

            if(!config.GenerateStaticDeserializeIntoInstance && config.GenerateDeserializeOnInstance)
                config = config with { GenerateStaticDeserializeIntoInstance =  config.GenerateDeserializeOnInstance };

            if (nm.FirstOrDefault(x => x.Key == nameof(GenerateStaticSerialize)).Value.Value is bool generateStaticSerialize)
                config = config with { GenerateStaticSerialize = generateStaticSerialize };

            if (nm.FirstOrDefault(x => x.Key == nameof(NameOfStaticSerialize)).Value.Value is string nameOfStaticSerialize)
                config = config with { NameOfStaticSerialize = nameOfStaticSerialize };

            if (nm.FirstOrDefault(x => x.Key == nameof(NameOfSerialize)).Value.Value is string nameOfSerialize)
                config = config with { NameOfSerialize = nameOfSerialize };

            config.ShouldContainMethodsCount = 
                AddB(config.GenerateDeserializeExtension, config.GenerateStaticDeserializeWithCtor) 
                + AddB(config.GenerateStaticDeserializeIntoInstance, config.GenerateDeserializeOnInstance)
                + AddB(config.GenerateStaticSerialize, true);

            return config;
        }

        private static int AddB(bool a, bool b) => (a?1:0) + (b?1:0);

    }

}
