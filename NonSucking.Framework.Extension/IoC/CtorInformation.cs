using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NonSucking.Framework.Extension.IoC
{
    internal class CtorInformation
    {
        private readonly ConstructorInfo ctor;
        private readonly TypeResolver resolver;
        private Func<object>[] arguments;

        public CtorInformation(TypeResolver resolver, ConstructorInfo ctor)
        {
            this.ctor = ctor;
            this.resolver = resolver;
            var parameterArray = ctor.GetParameters();

            arguments = new Func<object>[parameterArray.Length];

            foreach (var parameter in parameterArray)
            {
                var info = resolver.ResolveTypeAsInformation(parameter.ParameterType);

                if (info != null)
                {
                    arguments[parameter.Position] = () => info.Instance;
                }
                else if (parameter.IsOptional)
                {
                    arguments[parameter.Position] = () => null;
                }
                else
                {
                    throw new KeyNotFoundException(); //TODO: own Exception
                }
            }
        }

        public object Invoke() 
            => ctor.Invoke(arguments.Select(a => a()).ToArray());
    }
}
