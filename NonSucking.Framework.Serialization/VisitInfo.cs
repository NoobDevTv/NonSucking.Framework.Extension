using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace NonSucking.Framework.Serialization
{
    internal struct VisitInfo : IEquatable<VisitInfo>
    {
        public static readonly VisitInfo Empty = default;

        public INamedTypeSymbol TypeSymbol { get; private set; }
        public AttributeData Attribute { get; private set; }

        public IReadOnlyCollection<MemberInfo> Properties { get; init; }

        public VisitInfo(INamedTypeSymbol typeSymbol = default, AttributeData attribute = default, MemberInfo[] properties = default)
        {
            TypeSymbol = typeSymbol;
            Attribute = attribute;
            Properties = properties;
        }

        public override bool Equals(object obj) 
            => obj is VisitInfo info && Equals(info);
        public bool Equals(VisitInfo other) 
            => EqualityComparer<INamedTypeSymbol>.Default.Equals(TypeSymbol, other.TypeSymbol) 
            && EqualityComparer<AttributeData>.Default.Equals(Attribute, other.Attribute) 
            && EqualityComparer<IReadOnlyCollection<MemberInfo>>.Default.Equals(Properties, other.Properties);

        public override int GetHashCode()
        {
            var hashCode = -1923588403;
            hashCode = hashCode * -1521134295 + EqualityComparer<INamedTypeSymbol>.Default.GetHashCode(TypeSymbol);
            hashCode = hashCode * -1521134295 + EqualityComparer<AttributeData>.Default.GetHashCode(Attribute);
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyCollection<MemberInfo>>.Default.GetHashCode(Properties);
            return hashCode;
        }

        public static bool operator ==(VisitInfo left, VisitInfo right) => left.Equals(right);
        public static bool operator !=(VisitInfo left, VisitInfo right) => !(left == right);
    }
}
