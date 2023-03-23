using System;
using System.Collections.Generic;
using System.Linq;

using VaVare;
using VaVare.Models;

namespace NonSucking.Framework.Serialization
{
    public record GeneratedType(string Name, string DisplayName, bool IsRecord, bool IsValueType, bool IsAbstract, TypeParameter[] TypeParameters, TypeParameterConstraintClause[] TypeParameterConstraint, HashSet<GeneratedMethod> Methods, List<Modifiers> ClassModifiers, string? Summary = null, GeneratedType? ContainingType = null)
    {
        public bool HasGeneratedSerialization { get; set; }
        public virtual bool Equals(GeneratedType? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name
                   && IsRecord == other.IsRecord
                   && IsValueType == other.IsValueType
                   && TypeParameters.SequenceEqual(other.TypeParameters)
                   && (Methods.Equals(other.Methods)
                        || Methods.SequenceEqual(other.Methods))
                   && Equals(ContainingType, other.ContainingType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsRecord.GetHashCode();
                hashCode = (hashCode * 397) ^ IsValueType.GetHashCode();
                hashCode = (hashCode * 397) ^ (ContainingType?.GetHashCode() ?? 0);

                return hashCode;
            }
        }

    }

}
