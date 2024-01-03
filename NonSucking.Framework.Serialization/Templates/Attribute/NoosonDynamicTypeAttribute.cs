#nullable enable
using System;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    internal class NoosonDynamicTypeAttribute : Attribute
    {
        public NoosonDynamicTypeAttribute(params Type[] possibleTypes)
        {
            PossibleTypes = possibleTypes;
        }
        public Type[] PossibleTypes { get; }
        public Type? Resolver { get; set; }
    }
    
    internal interface INoosonRuntimeTypeResolver<TTypeIdentifier>
    {
        SerializerInformation<TWriter, TTypeIdentifier> Solve<TWriter>(Type type);
        DeserializerInformation<TReader, TTypeIdentifier> Resolve<TReader>(TTypeIdentifier identifier);
    }
    internal abstract class NoosonRuntimeTypeResolver<TTypeIdentifier>
        : INoosonRuntimeTypeResolver<TTypeIdentifier> where TTypeIdentifier : notnull
    {
        private readonly Dictionary<TTypeIdentifier, object> typeMap = new Dictionary<TTypeIdentifier, object>();
        private readonly Dictionary<Type, object> identifierMap = new Dictionary<Type, object>();

        protected abstract Type ResolveType<TReader>(TTypeIdentifier identifier);
        protected abstract TTypeIdentifier SolveType<TWriter>(Type type);

        DeserializerInformation<TReader, TTypeIdentifier> INoosonRuntimeTypeResolver<TTypeIdentifier>.Resolve<TReader>(TTypeIdentifier identifier)
        {
    #if NET6_0_OR_GREATER
            ref var item = ref CollectionsMarshal.GetValueRefOrAddDefault(typeMap, identifier, out var exists);
            if (!exists)
            {
                item = new DeserializerInformation<TReader, TTypeIdentifier>(identifier, ResolveType<TReader>(identifier));
            }
    #else
            if (!typeMap.TryGetValue(identifier, out var item))
            {
                item = new DeserializerInformation<TReader, TTypeIdentifier>(identifier, ResolveType<TReader>(identifier));
                typeMap.Add(identifier, item);
            }
    #endif
            return Unsafe.As<DeserializerInformation<TReader, TTypeIdentifier>>(item!);
        }

        SerializerInformation<TWriter, TTypeIdentifier> INoosonRuntimeTypeResolver<TTypeIdentifier>.Solve<TWriter>(Type type)
        {
    #if NET6_0_OR_GREATER
            ref var item = ref CollectionsMarshal.GetValueRefOrAddDefault(identifierMap, type, out var exists);
            if (!exists)
            {
                item = new SerializerInformation<TWriter, TTypeIdentifier>(SolveType<TWriter>(type), type);
            }
    #else
            if (!identifierMap.TryGetValue(type, out var item))
            {
                item = new SerializerInformation<TWriter, TTypeIdentifier>(SolveType<TWriter>(type), type);
                identifierMap.Add(type, item);
            }
    #endif
            return Unsafe.As<SerializerInformation<TWriter, TTypeIdentifier>>(item!);
        }
    }
}


