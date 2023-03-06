using Microsoft.CodeAnalysis;

using System.Collections.Generic;

namespace NonSucking.Framework.Serialization
{
    public record GlobalContext(Dictionary<string, GeneratedType> GeneratedTypes)
    {
        public GeneratedType? Resolve(ITypeSymbol? symbol)
        {
            return symbol is not null && TryResolve(symbol, out var type) ? type : null;
        }
        public bool TryResolve(ITypeSymbol symbol, out GeneratedType generatedType)
        {
            return GeneratedTypes.TryGetValue(symbol.ToDisplayString(), out generatedType);
        }
        public void Add(ITypeSymbol symbol, GeneratedType generatedType)
        {
            GeneratedTypes.Add(symbol.ToDisplayString(), generatedType);
        }
        /// <summary>
        /// Cleans everything, so no caches are left over
        /// </summary>
        public void Clean()
        {
            GeneratedTypes.Clear();
        }
    }

}
