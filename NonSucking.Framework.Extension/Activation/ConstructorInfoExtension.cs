using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NonSucking.Framework.Extension.Activation
{
    /// <summary>
    /// Extends the <see cref="ConstructorInfo"/> class with useful functions that are not included in the framework
    /// </summary>
    public static class ConstructorInfoExtension
    {
        /// <summary>
        /// Creates a new delegate with the <see cref="Expression"/> API for a <see cref="ConstructorInfo"/> instance
        /// </summary>
        /// <typeparam name="T">The type of instance to be created by the delegate</typeparam>
        /// <param name="constructorInfo">The constructor information from which a delegate is to be created</param>
        /// <returns>A delegat compiled with the <see cref="Expression"/> api which returns a new instance 
        /// of <see cref="MemberInfo.DeclaringType"/> as <typeparamref name="T"/></returns>
        public static Func<T> GetActivationDelegate<T>(this ConstructorInfo constructorInfo)
        {
            var parameters = constructorInfo.GetParameters().Select(p => Expression.Parameter(p.ParameterType));
            var body = Expression.New(constructorInfo, parameters);
            return Expression.Lambda<Func<T>>(body, parameters).Compile();
        }

        /// <summary>
        /// Creates a new delegate with the <see cref="Expression"/> API for a <see cref="ConstructorInfo"/> instance
        /// </summary>
        /// <param name="constructorInfo">The constructor information from which a delegate is to be created</param>
        /// <returns>A delegat compiled with the <see cref="Expression"/> api which returns a new instance of <see cref="MemberInfo.DeclaringType"/></returns>
        public static Delegate GetActivationDelegate(this ConstructorInfo constructorInfo)
        {
            var parameters = constructorInfo.GetParameters().Select(p => Expression.Parameter(p.ParameterType));
            var body = Expression.New(constructorInfo, parameters);
            return Expression.Lambda<Delegate>(body, parameters).Compile();
        }
    }
}
