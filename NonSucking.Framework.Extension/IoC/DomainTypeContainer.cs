using NonSucking.Framework.Extension.IoC.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.IoC
{
    public sealed class DomainTypeContainer : TypeContainerBase
    {
        public TypeDomain Domain { get; }
        public TypeDomainResolver Resolver { get; }

        private readonly StandaloneTypeContainer internalTypeContainer;

        public DomainTypeContainer(TypeDomain domain, TypeDomainResolver resolver)
        {
            Domain = domain;
            Resolver = resolver;
            internalTypeContainer = new StandaloneTypeContainer();
        }

        public override object Get(Type type)
            => GetOrNull(type) ?? throw new KeyNotFoundException();
        public override T Get<T>()
            => GetOrNull<T>() ?? throw new KeyNotFoundException();
        public object Get(Type type, TypeDomain domain)
            => GetOrNull(type, domain) ?? throw new KeyNotFoundException();
        public T Get<T>(TypeDomain domain) where T : class
            => GetOrNull<T>(domain) ?? throw new KeyNotFoundException();

        public override object GetOrNull(Type type)
            => internalTypeContainer.GetOrNull(type)
                ?? Resolver.Broadcast(Domain, type);
        public override T GetOrNull<T>()
            => internalTypeContainer.GetOrNull<T>()
                ?? Resolver.Broadcast<T>(Domain);
        public object GetOrNull(Type type, TypeDomain typeDomain)
        {
            if (typeDomain.Equals(Domain))
                return internalTypeContainer.GetOrNull(type);

            return Resolver.Resolve(Domain, type, typeDomain);
        }

        public T GetOrNull<T>(TypeDomain typeDomain) where T : class
        {
            if (typeDomain.Equals(Domain))
                return internalTypeContainer.GetOrNull<T>();

            return Resolver.Resolve<T>(Domain, typeDomain);
        }

        public override object GetUnregistered(Type type)
            => CreateObject(type)
               ?? throw new InvalidOperationException($"Can not create unregistered type of {type}");

        public override T GetUnregistered<T>()
            => CreateObject<T>()
               ?? throw new InvalidOperationException($"Can not create unregistered type of {typeof(T)}");


        public override void Register(Type registrar, Type type, InstanceBehaviour instanceBehaviour)
            => internalTypeContainer.Register(registrar, type, instanceBehaviour);
        public override void Register<T>(InstanceBehaviour instanceBehaviour = InstanceBehaviour.Instance)
            => internalTypeContainer.Register<T>(instanceBehaviour);
        public override void Register<TRegistrar, T>(InstanceBehaviour instanceBehaviour = InstanceBehaviour.Instance)
            => internalTypeContainer.Register<TRegistrar, T>(instanceBehaviour);
        public override void Register(Type registrar, Type type, object singelton)
            => internalTypeContainer.Register(registrar, type, singelton);
        public override void Register<T>(T singelton)
            => internalTypeContainer.Register(singelton);
        public override void Register<TRegistrar, T>(object singelton)
            => internalTypeContainer.Register<TRegistrar, T>(singelton);

        public override bool TryResolve(Type type, out object instance) 
            => throw new NotImplementedException();
        public override bool TryResolve<T>(out T instance) 
            => throw new NotImplementedException();

        public override void Dispose()
            => internalTypeContainer.Dispose();

        internal override void BuildCtorInformation(CtorInformation info)
            => internalTypeContainer.BuildCtorInformation(info);
    }
}
