﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static NonSucking.Framework.Serialization.GeneratedSerializerCode;

namespace NonSucking.Framework.Serialization
{
    public record MemberInfo(ITypeSymbol TypeSymbol, ISymbol Symbol, string Name, string Parent = "")
    {
        public Dictionary<string, string>? ScopeVariableNameMappings { get; set; }

        public string FullName =>  (string.IsNullOrWhiteSpace(Parent) || Parent == Consts.ThisName || Symbol.IsStatic)
                    ? Name
                    : $"{Parent}.{Name}";

        public string CreateUniqueName() => Helper.GetRandomNameFor(Name, Parent == Consts.ThisName ? "" : Parent);
    }
}