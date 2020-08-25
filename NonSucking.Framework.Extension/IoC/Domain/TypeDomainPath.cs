using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NonSucking.Framework.Extension.IoC.Domain
{
    public readonly struct TypeDomainPath : IEquatable<TypeDomainPath>
    {
        public static readonly TypeDomainPath Empty = default;

        private const string pathPattern = @"^([\w-]+\.)+([\w-]+)$|^([\w-]+)$";

        public readonly bool IsEmpty => this == default;

        public readonly IReadOnlyList<TypeDomainPathPart> Parts => parts;
        private readonly TypeDomainPathPart[] parts;

        public TypeDomainPath(params TypeDomainPathPart[] pathParts)
        {
            for (int i = 0; i < pathParts.Length; i++)
                if (pathParts[i].IsEmpty)
                    throw new ArgumentException();

            parts = pathParts;
        }

        public override readonly bool Equals(object obj)
            => obj is TypeDomainPath path && Equals(path);
        public readonly bool Equals(TypeDomainPath other)
            => EqualityComparer<TypeDomainPathPart[]>.Default.Equals(parts, other.parts);
        public override readonly int GetHashCode()
            => HashCode.Combine(parts);

        public override readonly string ToString() 
            => string.Join('.', parts);

        public static bool operator ==(TypeDomainPath left, TypeDomainPath right)
            => left.Equals(right);
        public static bool operator !=(TypeDomainPath left, TypeDomainPath right)
            => !(left == right);

        internal static TypeDomainPath Parse(string path)
        {
            var groupRegex = pathPattern;

            var match = Regex.Match(path, groupRegex);

            if (!match.Success)
                throw new FormatException("No valid path");

            if (match.Groups[3].Success)
            {
                return new TypeDomainPath(new TypeDomainPathPart(0, match.Groups[3].Value));
            }

            var pathElements = path.Split('.');
            var parts = new TypeDomainPathPart[pathElements.Length];

            for (uint i = 0; i < pathElements.Length; i++)
            {
                parts[i] = new TypeDomainPathPart(i, pathElements[i]);
            }

            return new TypeDomainPath(parts);
        }
    }
}
