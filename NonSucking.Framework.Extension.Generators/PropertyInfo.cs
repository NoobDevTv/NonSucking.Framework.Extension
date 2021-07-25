using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace NonSucking.Framework.Extension.Generators
{
    internal struct PropertyInfo : IEquatable<PropertyInfo>
    {
        public PropertyInfo(PropertyDeclarationSyntax propertyDeclarationSyntax, IPropertySymbol propertySymbol)
        {
            PropertyDeclarationSyntax = propertyDeclarationSyntax;
            PropertySymbol = propertySymbol;
        }

        public PropertyDeclarationSyntax PropertyDeclarationSyntax { get; set; }
        public IPropertySymbol PropertySymbol { get; set; }

        public override bool Equals(object obj) => obj is PropertyInfo info && Equals(info);
        public bool Equals(PropertyInfo other) => EqualityComparer<PropertyDeclarationSyntax>.Default.Equals(PropertyDeclarationSyntax, other.PropertyDeclarationSyntax) && EqualityComparer<IPropertySymbol>.Default.Equals(PropertySymbol, other.PropertySymbol);

        public override int GetHashCode()
        {
            var hashCode = 819844184;
            hashCode = hashCode * -1521134295 + EqualityComparer<PropertyDeclarationSyntax>.Default.GetHashCode(PropertyDeclarationSyntax);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPropertySymbol>.Default.GetHashCode(PropertySymbol);
            return hashCode;
        }

        public static bool operator ==(PropertyInfo left, PropertyInfo right) => left.Equals(right);
        public static bool operator !=(PropertyInfo left, PropertyInfo right) => !(left == right);
    }
}
