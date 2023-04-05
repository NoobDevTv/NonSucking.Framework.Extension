using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NonSucking.Framework.Extension.IoC
{
    public abstract class TypeContainerBase : ITypeContainer
    {
        public abstract void Dispose();
        public abstract object Get(Type type);
        public abstract T Get<T>() where T : class;
        public abstract object GetOrNull(Type type);
        public abstract T GetOrNull<T>() where T : class;
        public abstract object GetUnregistered(Type type);
        public abstract T GetUnregistered<T>() where T : class;
        public abstract void Register(Type registrar, Type type, InstanceBehaviour instanceBehaviour);
        public abstract void Register<T>(InstanceBehaviour instanceBehaviour = InstanceBehaviour.Instance) where T : class;
        public abstract void Register<TRegistrar, T>(InstanceBehaviour instanceBehaviour = InstanceBehaviour.Instance) where T : class;
        public abstract void Register(Type registrar, Type type, object singelton);
        public abstract void Register<T>(T singelton) where T : class;
        public abstract void Register<TRegistrar, T>(object singelton) where T : class;
        public abstract bool TryResolve(Type type, out object instance);
        public abstract bool TryResolve<T>(out T instance) where T : class;
        public abstract void Remove<T>() where T : class;
        public abstract void Remove(Type type);

        public virtual object CreateObject(Type type)
        {
            var tmpList = new List<object>();

            var constructors = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length);

            foreach (var constructor in constructors)
            {
                bool next = false;
                foreach (var parameter in constructor.GetParameters())
                {
                    if (TryResolve(parameter.ParameterType, out object instance))
                    {
                        tmpList.Add(instance);
                    }
                    else if (!parameter.IsOptional)
                    {
                        tmpList.Clear();
                        next = true;
                        break;
                    }
                    else if (parameter.IsOptional && parameter.HasDefaultValue)
                    {
                        tmpList.Add(parameter.DefaultValue);
                    }
                }

                if (next)
                    continue;

                return constructor.Invoke(tmpList.ToArray());
            }

            if (constructors.Count() < 1)
            {
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public virtual T CreateObject<T>() where T : class
            => (T)CreateObject(typeof(T));

        internal abstract void BuildCtorInformation(CtorInformation info);

        internal void BuildCtorInformation(IDictionary<Type, Type> typeRegister, 
            IDictionary<Type, TypeInformation> typeInformationRegister, CtorInformation info)
        {
            var func = info.Length > 0 ? (Action<ParameterInfo, TypeInformation>)info.Update : info.Add;
            foreach (var parameter in info.GetParameters())
            {
                if (typeRegister.TryGetValue(parameter.ParameterType, out var searchType)
                   && typeInformationRegister.TryGetValue(searchType, out var typeInformation))
                {
                    func(parameter, typeInformation);
                }
                else
                {
                    func(parameter, null);
                }
            }
        }

        internal IEnumerable<CtorInformation> GetCtorInformations(Type type)
        {
            var constructors = type
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length);

            foreach (var constructor in constructors)
            {
                var info = new CtorInformation(constructor);
                BuildCtorInformation(info);

                yield return info;
            }
        }

    }
}
