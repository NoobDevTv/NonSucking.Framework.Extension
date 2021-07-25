using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace NonSucking.Framework.Extension.Generators
{
    internal struct VisitInfo : IEquatable<VisitInfo>
    {
        public ClassDeclarationSyntax ClassDeclaration { get; private set; }
        public INamedTypeSymbol TypeSymbol { get; private set; }
        public AttributeData Attribute { get; private set; }

        public List<TypeGroupInfo> Properties { get; }

        public VisitInfo(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol typeSymbol, AttributeData attribute, List<TypeGroupInfo> properties)
        {
            ClassDeclaration = classDeclaration;
            TypeSymbol = typeSymbol;
            Attribute = attribute;
            Properties = properties;
        }

        public override bool Equals(object obj) => obj is VisitInfo info && Equals(info);
        public bool Equals(VisitInfo other) => EqualityComparer<ClassDeclarationSyntax>.Default.Equals(ClassDeclaration, other.ClassDeclaration) && EqualityComparer<INamedTypeSymbol>.Default.Equals(TypeSymbol, other.TypeSymbol) && EqualityComparer<AttributeData>.Default.Equals(Attribute, other.Attribute) && EqualityComparer<List<TypeGroupInfo>>.Default.Equals(Properties, other.Properties);

        public override int GetHashCode()
        {
            var hashCode = -1923588403;
            hashCode = hashCode * -1521134295 + EqualityComparer<ClassDeclarationSyntax>.Default.GetHashCode(ClassDeclaration);
            hashCode = hashCode * -1521134295 + EqualityComparer<INamedTypeSymbol>.Default.GetHashCode(TypeSymbol);
            hashCode = hashCode * -1521134295 + EqualityComparer<AttributeData>.Default.GetHashCode(Attribute);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<TypeGroupInfo>>.Default.GetHashCode(Properties);
            return hashCode;
        }

        public static bool operator ==(VisitInfo left, VisitInfo right) => left.Equals(right);
        public static bool operator !=(VisitInfo left, VisitInfo right) => !(left == right);
    }
}
