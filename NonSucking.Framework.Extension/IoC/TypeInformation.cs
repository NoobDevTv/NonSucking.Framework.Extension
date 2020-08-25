using System;
using System.Collections.Generic;
using System.Linq;

namespace NonSucking.Framework.Extension.IoC
{
    internal class TypeInformation
    {
        public InstanceBehaviour Behaviour { get; set; }
        public object Instance => CreateObject();

        public bool Completed { get; private set; }

        private readonly TypeContainerBase typeContainer;
        private readonly Type type;
        private object singeltonInstance;
        private readonly List<CtorInformation> ctors;

        public TypeInformation(TypeContainerBase container,
            Type type, InstanceBehaviour instanceBehaviour, object instance = null)
        {
            this.type = type;
            Behaviour = instanceBehaviour;
            typeContainer = container;
            singeltonInstance = instance;
            ctors = container
                .GetCtorInformations(type)
                .OrderByDescending(ctor => ctor.Length)
                .ToList();

            Completed = !ctors.Any(c => !c.IsComplete);
        }

        private object CreateObject()
        {
            if (Behaviour == InstanceBehaviour.Singleton && singeltonInstance != null)
                return singeltonInstance;

            var obj = ctors.FirstOrDefault(c => c.IsComplete)?.Invoke();

            if (Behaviour == InstanceBehaviour.Singleton)
            {
                singeltonInstance = obj;
                Completed = true;
            }

            return obj;
        }

        public void RecreateUncompleteCtors()
        {
            if (Completed)
                return;

            foreach (var ctor in ctors.Where(ctor => !ctor.IsComplete))
                typeContainer.BuildCtorInformation(ctor);

            Completed = !ctors.Any(c => !c.IsComplete);
        }
    }

}
