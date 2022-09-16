using Microsoft.CodeAnalysis;

namespace NonSucking.Framework.Serialization
{
    public record struct MemberInfo(ITypeSymbol TypeSymbol, ISymbol Symbol, string Name, string Parent = "")
    {
        public string FullName => (string.IsNullOrWhiteSpace(Parent) || Parent == "this" || Symbol.IsStatic) ? Name : $"{Parent}.{Name}";

        public string CreateUniqueName() => Helper.GetRandomNameFor(Name, Parent == "this" ? "" : Parent);
    }
}
