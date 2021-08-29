using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace NonSucking.Framework.Extension.Generators
{
    internal record MemberInfo(ITypeSymbol TypeSymbol, ISymbol Symbol, string Name);

    //internal struct PropertyInfo 
    //{
    //    public PropertyInfo(ITypeSymbol propertySymbol, string name)
    //    {
    //        PropertySymbol = propertySymbol;
    //    }

    //    public IPropertySymbol PropertySymbol { get; set; }

  
    //}
}
