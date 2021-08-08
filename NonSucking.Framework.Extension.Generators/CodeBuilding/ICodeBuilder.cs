using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.Generators.CodeBuilding
{
    interface ICodeBuilder
    {
    }

    public static class ICodeBuilderExtension
    {

        public ICodeBuilder AddUsing(this ICodeBuilder builder) { }
        public INameSpaceBuilder AddNamespace(this ICodeBuilder builder) { }
    }

}
