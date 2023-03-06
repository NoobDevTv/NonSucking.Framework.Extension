using System;
using System.Collections.Generic;
using System.Linq;

using VaVare.Models;

namespace NonSucking.Framework.Serialization
{
    public record GeneratedType(string? Namespace, string Name, string DisplayName, bool IsRecord, bool IsValueType, TypeParameter[] TypeParameters, TypeParameterConstraintClause[] TypeParameterConstraint, HashSet<GeneratedMethod> Methods, HashSet<string> Usings, string? Summary = null, GeneratedType? ContainingType = null)
    {
        public virtual bool Equals(GeneratedType? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name
                   && Namespace == other.Namespace
                   && IsRecord == other.IsRecord
                   && IsValueType == other.IsValueType
                   && TypeParameters.SequenceEqual(other.TypeParameters)
                   && (Methods.Equals(other.Methods)
                        || Methods.SequenceEqual(other.Methods))
                   && (Usings.Equals(other.Usings)
                        || Usings.SequenceEqual(other.Usings))
                   && Equals(ContainingType, other.ContainingType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (Namespace?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ IsRecord.GetHashCode();
                hashCode = (hashCode * 397) ^ IsValueType.GetHashCode();
                hashCode = (hashCode * 397) ^ (ContainingType?.GetHashCode() ?? 0);

                return hashCode;
            }
        }

    }

}
