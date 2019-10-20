using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.IoC
{
    public sealed class StandaloneTypeResolver : TypeResolver
    {
        private readonly Dictionary<Type, TypeInformation> typeInformationRegister;
        private readonly Dictionary<Type, TypeInformation> typeRegister;

        public StandaloneTypeResolver()
        {
            typeInformationRegister = new Dictionary<Type, TypeInformation>();
            typeRegister = new Dictionary<Type, TypeInformation>();
        }

        internal override TypeInformation ResolveTypeAsInformation(Type type)
        {
            return typeRegister[type];
        }
    }
}
