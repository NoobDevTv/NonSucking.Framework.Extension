using System;
using System.Collections.Generic;
using System.Linq;

using VaVare;
using VaVare.Models;

namespace NonSucking.Framework.Serialization
{
    public record GeneratedMethod(GeneratedMethodParameter? ReturnType, string Name, List<GeneratedMethodParameter> Parameters, List<Modifiers> Modifier, TypeParameter[] TypeParameters, TypeParameterConstraintClause[] TypeParameterConstraints, GeneratedSerializerCode Body, string? Summary)
    {
        public bool IsVirtual => Modifier.Any(x => x == Modifiers.Virtual);
        public bool IsAbstract => Modifier.Any(x => x == Modifiers.Abstract);
        public bool IsStatic => Modifier.Any(x => x == Modifiers.Static);
        public virtual bool Equals(GeneratedMethod? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name
                && (Parameters.Equals(other.Parameters)
                    || Parameters.SequenceEqual(other.Parameters))
                && (Modifier.Equals(other.Modifier)
                    || Modifier.SequenceEqual(other.Modifier));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                return hashCode;
            }
        }
    }

}
