using NonSucking.Framework.Extension.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NonSucking.Framework.Extension.IoC
{
    public sealed class StandaloneTypeContainer : TypeContainerBase
    {
        private readonly Dictionary<Type, TypeInformation> typeInformationRegister;
        private readonly Dictionary<Type, Type> typeRegister;
        private readonly HashSet<TypeInformation> uncompletedList;

        public StandaloneTypeContainer()
        {
            typeInformationRegister = new Dictionary<Type, TypeInformation>();
            typeRegister = new Dictionary<Type, Type>();
            uncompletedList = new HashSet<TypeInformation>();
        }

        public override void Register(Type registrar, Type type, InstanceBehaviour instanceBehaviour)
        {
            TypeInformation registerInfo = null;
            if (!typeInformationRegister.ContainsKey(type))
            {
                registerInfo = new TypeInformation(this, type, instanceBehaviour);
                typeInformationRegister.Add(type, registerInfo);
            }

            typeRegister.Add(registrar, type);

            var removelist = new List<TypeInformation>();
            foreach (TypeInformation typeInformation in uncompletedList)
            {
                typeInformation.RecreateUncompleteCtors();
                if (typeInformation.Completed)
                    removelist.Add(typeInformation);
            }

            uncompletedList.RemoveWhere(t => removelist.Contains(t));
            if (registerInfo != null && !registerInfo.Completed)
                uncompletedList.Add(registerInfo);
        }
        public override void Register<T>(InstanceBehaviour instanceBehaviour = InstanceBehaviour.Instance) where T : class
            => Register(typeof(T), typeof(T), instanceBehaviour);
        public override void Register<TRegistrar, T>(InstanceBehaviour instanceBehaviour = InstanceBehaviour.Instance) where T : class
            => Register(typeof(TRegistrar), typeof(T), instanceBehaviour);
        public override void Register(Type registrar, Type type, object singelton)
        {
            if (!typeInformationRegister.ContainsKey(type))
                typeInformationRegister.Add(type, new TypeInformation(this, type, InstanceBehaviour.Singleton, singelton));

            typeRegister.Add(registrar, type);

            foreach (TypeInformation typeInformation in typeInformationRegister.Values.Where(t => !t.Completed))
            {
                typeInformation.RecreateUncompleteCtors();
            }
        }
        public override void Register<T>(T singelton) where T : class
            => Register(typeof(T), typeof(T), singelton);
        public override void Register<TRegistrar, T>(object singelton) where T : class
            => Register(typeof(TRegistrar), typeof(T), singelton);

        public override bool TryResolve(Type type, out object instance)
        {
            instance = GetOrNull(type);
            return instance != null;
        }
        public override bool TryResolve<T>(out T instance) where T : class
        {
            var result = TryResolve(typeof(T), out var obj);
            instance = (T)obj;
            return result;
        }

        public override object Get(Type type)
            => GetOrNull(type) ?? throw new KeyNotFoundException($"Type {type} was not found in Container");

        public override T Get<T>() where T : class
            => (T)Get(typeof(T));

        public override object GetOrNull(Type type)
        {
            if (typeRegister.TryGetValue(type, out Type searchType))
            {
                if (typeInformationRegister.TryGetValue(searchType, out TypeInformation typeInformation))
                    return typeInformation.Instance;
            }
            return null;
        }
        public override T GetOrNull<T>() where T : class
            => (T)GetOrNull(typeof(T));

        public override object GetUnregistered(Type type)
            => GetOrNull(type)
                ?? CreateObject(type)
                ?? throw new InvalidOperationException($"Can not create unregistered type of {type}");

        public override T GetUnregistered<T>() where T : class
            => (T)GetUnregistered(typeof(T));

        public override void Dispose()
        {
            typeRegister.Clear();
            typeInformationRegister.Values
                .Where(t => t.Behaviour == InstanceBehaviour.Singleton && t.Instance != this)
                .Select(t => t.Instance as IDisposable)
                .ToList()
                .ForEach(i => i?.Dispose());

            typeInformationRegister.Clear();
        }

        internal override void BuildCtorInformation(CtorInformation info)
            => BuildCtorInformation(typeRegister, typeInformationRegister, info);
    }
}
