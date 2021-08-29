using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace NonSucking.Framework.Extension.Generators
{
    internal struct TypeGroupInfo : IEquatable<TypeGroupInfo>
    {
        public TypeGroupInfo(TypeSyntax typeSyntax, SymbolInfo typeSymbol, MemberInfo[] properties)
        {
            TypeSyntax = typeSyntax;
            TypeSymbol = typeSymbol;
            Properties = properties;
        }

        public TypeSyntax TypeSyntax { get; set; }
        public SymbolInfo TypeSymbol { get; set; }
        public MemberInfo[] Properties { get; private set; }

        public override bool Equals(object obj) => obj is TypeGroupInfo info && Equals(info);
        public bool Equals(TypeGroupInfo other) => EqualityComparer<TypeSyntax>.Default.Equals(TypeSyntax, other.TypeSyntax) && TypeSymbol.Equals(other.TypeSymbol) && EqualityComparer<MemberInfo[]>.Default.Equals(Properties, other.Properties);

        public override int GetHashCode()
        {
            var hashCode = -1055744729;
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeSyntax>.Default.GetHashCode(TypeSyntax);
            hashCode = hashCode * -1521134295 + TypeSymbol.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<MemberInfo[]>.Default.GetHashCode(Properties);
            return hashCode;
        }

        public static bool operator ==(TypeGroupInfo left, TypeGroupInfo right) => left.Equals(right);
        public static bool operator !=(TypeGroupInfo left, TypeGroupInfo right) => !(left == right);
    }
}
