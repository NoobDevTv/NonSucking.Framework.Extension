using System;
using System.Collections.Generic;
using System.Linq;

using VaVare;

namespace NonSucking.Framework.Serialization
{
    /*
    SUTMessage
        static Deserialize(Reader)
        static Deserialize(Reader, out, out, out)
    NullableTest
    HansPeter
     */
    public record GeneratedMethodParameter(string Type, string Name, HashSet<ParameterModifiers> Modifier, string? Summary)
    {
        public virtual bool Equals(GeneratedMethodParameter? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Type == other.Type
                && Name == other.Name
                && (Equals(Modifier, other.Modifier)
                    || Modifier.SequenceEqual(other.Modifier));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Type.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                return hashCode;
            }
        }
    }

}
