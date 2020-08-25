using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NonSucking.Framework.Extension.IoC.Domain
{
    public readonly struct TypeDomain : IEquatable<TypeDomain>
    {
        public static readonly TypeDomain Empty = default;

        private const string domainPattern = @"^([\w-]+\.)+([\w-]+)$|^([\w-]+)$";
        private const string namePattern = @"^([\w-]+)$";

        public readonly bool IsEmpty => this == default;
        public readonly TypeDomainPath Path { get;  }
        public readonly string Name { get;  }

        public TypeDomain(string path, string name) 
            : this(TypeDomainPath.Parse(path), name)
        {
        }
        public TypeDomain(TypeDomainPath path, string name)
        {
            if (!Regex.IsMatch(name, namePattern))
                throw new FormatException("Name is invalid");

            Path = path;
            Name = name;
        }

        public override readonly bool Equals(object obj) 
            => obj is TypeDomain domain && Equals(domain);
        public readonly bool Equals(TypeDomain other) 
            => Path == other.Path && Name == other.Name;
        public override readonly int GetHashCode() 
            => HashCode.Combine(Path, Name);

        public override readonly string ToString() 
            => Path + ":" + Name;

        public static bool operator ==(TypeDomain left, TypeDomain right)
            => left.Equals(right);
        public static bool operator !=(TypeDomain left, TypeDomain right)
            => !(left == right);

        public static TypeDomain Parse(string typeDomain)
        {
            var match = Regex.Match(typeDomain, domainPattern);

            if (!match.Success)
                throw new FormatException("Could not parse"); //New

            if (match.Groups[5].Success) //Group5 then is only the name
            {
                return new TypeDomain(TypeDomainPath.Empty, match.Groups[5].Value);
            }

            var groupFor = match.Groups[4];
            var name = groupFor.Value;
            var path = typeDomain.Substring(0, groupFor.Index - 1);
           return new TypeDomain(TypeDomainPath.Parse(path), name);
        }
    }
}
