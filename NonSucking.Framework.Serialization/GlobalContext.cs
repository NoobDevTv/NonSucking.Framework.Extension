using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NonSucking.Framework.Serialization
{

    public record GlobalContext(Dictionary<string, GeneratedFile> GeneratedFiles, Compilation Compilation, NoosonConfig Config)
    {
        Dictionary<AssemblyIdentity, NoosonConfig> ThirdPartyConfigs { get; set; } = new();
        public GeneratedFile? Resolve(ITypeSymbol? symbol)
        {
            return symbol is not null && TryResolve(symbol, out var type) ? type : null;
        }
        public bool TryResolve(ITypeSymbol symbol, out GeneratedFile generatedType)
        {
            return GeneratedFiles.TryGetValue(symbol.OriginalDefinition.ToDisplayString(), out generatedType);
        }
        public void Add(ITypeSymbol symbol, GeneratedFile generatedType)
        {
            GeneratedFiles.Add(symbol.OriginalDefinition.ToDisplayString(), generatedType);
        }

        public NoosonConfig GetConfigForSymbol(ISymbol symbol)
        {
            if(symbol.ContainingAssembly is null)
            {
                return new NoosonConfig(true);
            }

            if (!ThirdPartyConfigs.TryGetValue(symbol.ContainingAssembly.Identity, out var config))
            {
                var attribute = symbol.ContainingAssembly.GetAttribute(AttributeTemplates.NoosonConfiguration);
                return ThirdPartyConfigs[symbol.ContainingAssembly.Identity] = new NoosonConfig(true).ReloadFrom(attribute);
            }
            return config;
        }
        /// <summary>
        /// Cleans everything, so no caches are left over
        /// </summary>
        public void Clean()
        {
            ThirdPartyConfigs.Clear();
            GeneratedFiles.Clear();
        }
    }

}
