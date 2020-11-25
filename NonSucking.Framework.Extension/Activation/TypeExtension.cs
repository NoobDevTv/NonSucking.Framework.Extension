using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace NonSucking.Framework.Extension.Activation
{
    /// <summary>
    /// Extends the <see cref="Type"/> class with useful functions that are not included in the framework
    /// </summary>
    public static class TypeExtension
    {
        /// <summary>
        /// Creates a new delegate with the <see cref="Expression"/> API for a <see cref="Type"/> instance
        /// </summary>
        /// <typeparam name="T">The type of instance to be created by the delegate</typeparam>
        /// <param name="type">The constructor information from which a delegate is to be created</param>
        /// <returns>A delegat compiled with the <see cref="Expression"/> api which returns a new instance 
        /// of <paramref name="type"/> as <typeparamref name="T"/></returns>
        public static Func<T> GetActivationDelegate<T>(this Type type)
        {
            var body = Expression.New(type);
            return Expression.Lambda<Func<T>>(body).Compile();
        }

        /// <summary>
        /// Creates a new delegate with the <see cref="Expression"/> API for a <see cref="Type"/> instance
        /// </summary>
        /// <typeparam name="T">The type of instance to be created by the delegate</typeparam>
        /// <param name="type">The constructor information from which a delegate is to be created</param>
        /// <returns>A delegat compiled with the <see cref="Expression"/> api which returns a new instance 
        /// of <paramref name="type"/> as <typeparamref name="T"/></returns>
        public static Delegate GetActivationDelegate(this Type type)
        {
            var body = Expression.New(type);
            return Expression.Lambda<Delegate>(body).Compile();
        }
    }
}
