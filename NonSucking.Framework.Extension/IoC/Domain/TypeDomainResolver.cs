using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.IoC.Domain
{
    public sealed class TypeDomainResolver
    {
        public TypeDomainPath Path { get; }
        public TypeDomainResolver Parent { get; }

        public TypeDomainResolver(TypeDomainPath path, TypeDomainResolver parent = null)
        {
            if (path.IsEmpty)
                throw new ArgumentException();

            Path = path;
            Parent = parent;
        }
        public TypeDomainResolver(string path, TypeDomainResolver parent = null) 
            : this(TypeDomainPath.Parse(path), parent)
        {
           
        }

        internal object Broadcast(TypeDomain domain, Type type) => throw new NotImplementedException();
        internal T Broadcast<T>(TypeDomain domain) where T : class => throw new NotImplementedException();
        internal object Resolve(TypeDomain domain, Type type, TypeDomain typeDomain) => throw new NotImplementedException();
        internal T Resolve<T>(TypeDomain domain, TypeDomain typeDomain) where T : class => throw new NotImplementedException();
    }
}
