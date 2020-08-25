using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NonSucking.Framework.Extension.IoC.Domain
{
    public readonly struct TypeDomainPathPart : IEquatable<TypeDomainPathPart>
    {
        public static readonly TypeDomainPathPart Empty = default;

        private const string literalPattern = @"^([\w-]+)$";

        public readonly bool IsEmpty => this == default;

        public readonly uint Index { get; }
        public readonly string Literal { get; }

        public TypeDomainPathPart(uint index, string literal)
        {
            if (!Regex.IsMatch(literal, literalPattern))
                throw new FormatException();

            Index = index;
            Literal = literal;
        }

        public override readonly bool Equals(object obj) 
            => obj is TypeDomainPathPart part && Equals(part);
        public readonly bool Equals(TypeDomainPathPart other) 
            => Index == other.Index && Literal == other.Literal;
        public override readonly int GetHashCode() 
            => HashCode.Combine(Index, Literal);

        public override readonly string ToString() 
            => Literal;

        public static bool operator ==(TypeDomainPathPart left, TypeDomainPathPart right) 
            => left.Equals(right);
        public static bool operator !=(TypeDomainPathPart left, TypeDomainPathPart right) 
            => !(left == right);
    }
}
