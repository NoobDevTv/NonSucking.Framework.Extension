﻿using System;
using System.Collections.Generic;
using System.Linq;

using VaVare;

namespace NonSucking.Framework.Serialization
{
    public record GeneratedMethodParameter(string Type, string Name, HashSet<ParameterModifiers> Modifier, string? Summary, GeneratedSerializerCode.SerializerVariable? SerializerVariable = null)
    {
        public bool IsOut => Modifier.Any(x => x == ParameterModifiers.Out);
        public bool IsRef => Modifier.Any(x => x == ParameterModifiers.Ref);
        public bool IsThis => Modifier.Any(x => x == ParameterModifiers.This);

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
