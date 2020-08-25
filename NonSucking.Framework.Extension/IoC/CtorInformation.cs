using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NonSucking.Framework.Extension.IoC
{
    internal class CtorInformation
    {
        public bool IsComplete { get; private set; }
        public int Length => parameters.Count;

        private readonly ConstructorInfo constructor;
        private readonly Dictionary<ParameterInfo, TypeInformation> parameters;

        public CtorInformation(ConstructorInfo constructor)
        {
            parameters = new Dictionary<ParameterInfo, TypeInformation>();
            this.constructor = constructor;
            IsComplete = true;
        }

        internal void Add(ParameterInfo parameter, TypeInformation typeInformation)
        {
            parameters.Add(parameter, typeInformation);

            if (typeInformation == null && !parameter.IsOptional)
                IsComplete = false;
        }

        internal void Update(ParameterInfo parameter, TypeInformation typeInformation)
        {
            parameters[parameter] = typeInformation;

            if (typeInformation == null && !parameter.IsOptional)
                IsComplete = false;
            else
                IsComplete = !parameters.Any(info => info.Value == null && !info.Key.IsOptional);
        }

        internal void Clear()
        {
            parameters.Clear();
        }

        internal IEnumerable<ParameterInfo> GetParameters()
            => constructor.GetParameters();

        internal object Invoke()
        {
            if (!IsComplete)
                throw new InvalidOperationException();

            return constructor.Invoke(parameters
                .OrderBy(v => v.Key.Position)
                .Select(v => v.Value)
                .Select(info =>
                {
                    if (info == null)
                        return null;
                    else
                        return info.Instance;
                }).ToArray());
        }

    }
}
