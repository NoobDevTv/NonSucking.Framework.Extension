using Microsoft.CodeAnalysis;

namespace NonSucking.Framework.Serialization
{
    internal record MemberInfo(ITypeSymbol TypeSymbol, ISymbol Symbol, string Name, string Parent = "")
    {
        public string FullName => string.IsNullOrWhiteSpace(Parent) ? Name : $"{Parent}.{Name}";
    }
}
