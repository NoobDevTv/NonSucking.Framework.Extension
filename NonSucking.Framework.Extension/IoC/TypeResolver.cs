using System;

namespace NonSucking.Framework.Extension.IoC
{
    public abstract class TypeResolver
    {
        internal abstract TypeInformation ResolveTypeAsInformation(Type type);
    }
}