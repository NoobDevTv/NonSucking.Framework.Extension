using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace NonSucking.Framework.Serialization.Advanced;


internal class MethodResolver
{
    private static readonly Dictionary<string, List<MethodInfo>> RegisteredExtensionMethods = new();
    private static readonly HashSet<Type> AlreadyRegistered = new();
    private static readonly HashSet<AssemblyNameKey> AlreadyRegisteredAssemblies = new();

    readonly struct AssemblyNameKey : IEquatable<AssemblyNameKey>
    {
        public AssemblyNameKey(AssemblyName assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public AssemblyName AssemblyName { get; }

        public bool Equals(AssemblyNameKey other)
        {
            return AssemblyName.FullName.Equals(other.AssemblyName.FullName);
        }

        public override bool Equals(object? obj)
        {
            return obj is AssemblyNameKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return AssemblyName.FullName.GetHashCode();
        }
        
        public static implicit operator AssemblyNameKey(AssemblyName name)
        {
            return new(name);
        }
    }

    static MethodResolver()
    {
        // Default resolve extension methods
        AnalyzeExtensionMethodsRecurse(Assembly.GetCallingAssembly());
        AnalyzeExtensionMethodsRecurse(Assembly.GetEntryAssembly());
    }
    internal static IEnumerable<MethodInfo> GetRegisteredExtensionMethods(string name)
    {
        return RegisteredExtensionMethods.TryGetValue(name, out var methods)
            ? methods
            : Array.Empty<MethodInfo>();
    }

    internal static void AnalyzeExtensionMethods(Type type)
    {
        if (!type.IsClass || !type.IsSealed || !type.IsAbstract || type.GetCustomAttribute<ExtensionAttribute>() is null)
            return;

        if (!AlreadyRegistered.Add(type))
            return;

        var methods = type.GetMethods();
        Array.Sort(methods, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        var previousMethodName = "";
        List<MethodInfo> resolvedMethods = null!;
        foreach (var m in methods)
        {
            if (m.GetCustomAttribute<ExtensionAttribute>() is null)
                continue;
            if (previousMethodName != m.Name)
            {
                ref var refList = ref CollectionsMarshal.GetValueRefOrAddDefault(RegisteredExtensionMethods, m.Name, out var exists);
                if (exists)
                    resolvedMethods = refList!;
                else
                    refList = resolvedMethods = new List<MethodInfo>();
                previousMethodName = m.Name;
            }

            resolvedMethods.Add(m);
        }
    }

    internal static void AnalyzeExtensionMethodsRecurse(Assembly? assembly)
    {
        if (assembly is null)
            return;
        AnalyzeExtensionMethods(assembly);
        foreach (var referencedName in assembly.GetReferencedAssemblies())
        {
            if (AlreadyRegisteredAssemblies.Contains(referencedName))
                continue;
            var referencedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(referencedName);
            AnalyzeExtensionMethodsRecurse(referencedAssembly);
        }
    }
    internal static void AnalyzeExtensionMethods(Assembly assembly)
    {
        if (!AlreadyRegisteredAssemblies.Add(assembly.GetName()))
            return;
        foreach (var t in assembly.GetTypes())
        {
            AnalyzeExtensionMethods(t);
        }
    }
    private static Type[] MatchGenericArguments(MethodInfo x, ParameterInfo[] parameters, Type[] parameterTypes, Type returnType)
    {
        // TODO: cannot match generic parameters that are not used as parameter types
        var genArguments = x.GetGenericArguments();
        var matchedParameters = new Type[genArguments.Length];

        void MatchNew(int index, Type newlyMatched)
        {
            var previouslyMatched = matchedParameters[index];
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (previouslyMatched is null || newlyMatched.IsAssignableFrom(previouslyMatched))
            {
                matchedParameters[index] = newlyMatched;
            }
        }
        
        for (var index = 0; index < genArguments.Length; index++)
        {
            var genArg = genArguments[index];

            if (x.ReturnType == genArg)
            {
                MatchNew(index, returnType);
            }

            for (var pIndex = 0; pIndex < parameters.Length; pIndex++)
            {
                var genParam = parameters[pIndex];
                if (genParam.ParameterType != genArg)
                    continue;
                var newlyMatched = parameterTypes[pIndex];
                MatchNew(index, newlyMatched);
            }
        }

        return matchedParameters;
    }

    private static IEnumerable<MethodInfo> SelectMethods(IEnumerable<MethodInfo> methodInfos, Type[] parameterTypes, Type? returnType)
    {
        return methodInfos.Select(x =>
                           {
                               try
                               {
                                   var parameters = x.GetParameters();
                                   if (parameters.Length != parameterTypes.Length)
                                       return null;
                                   if (x.IsGenericMethodDefinition)
                                   {
                                       var matched = MatchGenericArguments(x, parameters, parameterTypes, returnType ?? typeof(void));
                                       var genMethod = x.MakeGenericMethod(matched);
                                       if (genMethod.ReturnType != (returnType ?? typeof(void)))
                                           return null;
                                       return genMethod;
                                   }

                                   return x;
                               }
                               catch (ArgumentException)
                               {
                                   return null;
                               }
                           }).Where(x => x is not null).OfType<MethodInfo>();
    }
    internal static MethodInfo? GetBestMatch(Type type, string methodName, BindingFlags bindingFlags, Type[] parameterTypes, Type? returnType)
    {
        var methods = SelectMethods(type.GetMethods(bindingFlags)
            .Where(x => x.Name == methodName), parameterTypes, returnType).ToArray();

        if (GetPerfectMatch(parameterTypes, methods) is { } perfectMatch)
            return perfectMatch;

        // ReSharper disable once CoVariantArrayConversion
        var bestInstanceMethod = methods.Length == 0 ? null : Type.DefaultBinder.SelectMethod(bindingFlags, methods, parameterTypes, null) as MethodInfo;
        if (bestInstanceMethod is null)
        {
            var parameterTypesWithThis = parameterTypes.Prepend(type).ToArray();
            var extMethods = SelectMethods(GetRegisteredExtensionMethods(methodName), parameterTypesWithThis, returnType).ToArray();
            if (GetPerfectMatch(parameterTypesWithThis, extMethods) is { } perfectExtMatch)
                return perfectExtMatch;
            if (extMethods.Length == 0)
                return null;
            // ReSharper disable once CoVariantArrayConversion
            return Type.DefaultBinder.SelectMethod(bindingFlags, extMethods, parameterTypesWithThis, null) as MethodInfo;
        }

        return bestInstanceMethod;
    }

    private static MethodInfo? GetPerfectMatch(Type[] parameterTypes, MethodInfo[] methods)
    {
        var perfectMatch = methods.FirstOrDefault(
            x =>
            {
                if (x.IsGenericMethod)
                    return false;
                var parameters = x.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            });
        return perfectMatch;
    }
}