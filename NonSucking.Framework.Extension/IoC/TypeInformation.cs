using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NonSucking.Framework.Extension.IoC
{
    internal class TypeInformation
    {
        public object Instance { get; internal set; }

        private readonly TypeResolver resolver;
        private readonly Type type;
        private readonly IEnumerable<CtorInformation> ctors;

        public TypeInformation(TypeResolver resolver, Type type)
        {
            this.resolver = resolver;
            this.type = type;
            ctors = CreateCtor(type.GetConstructors());
        }

        private IEnumerable<CtorInformation> CreateCtor(ConstructorInfo[] constructorInfo)
        {
            foreach (var ctor in constructorInfo)
                yield return new CtorInformation(resolver, ctor);
        }
    }
}
