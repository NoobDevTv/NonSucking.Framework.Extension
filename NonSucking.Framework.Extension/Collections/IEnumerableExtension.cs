using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.Collections
{
    /// <summary>
    /// Extends the <see cref="IEnumerable{T}"/> interface by several useful functions.
    /// </summary>
    public static class IEnumerableExtension
    {
        /// <summary>
        /// Executes the specified function for each object in the iteration.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the function is to be executed.</param>
        /// <param name="function">The function to be executed for each object</param>
        /// <returns>An enumerator with the result of the function</returns>
        public static IEnumerable<T> OnEach<T>(this IEnumerable<T> enumerable, Func<T, T> function)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var item in enumerable)
                yield return function(item);
        }
        /// <summary>
        /// Executes the specified action for each object in the iteration.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="action">The action to be executed for each object</param>
        /// <returns>An enumerator with object of the <paramref name="enumerable"/></returns>
        public static IEnumerable<T> OnEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var item in enumerable)
            {
                action(item);
                yield return item;
            }
        }

        /// <summary>
        /// Executes the specified action for each object in the iteration.
        /// 
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="action">The action to be executed for each object</param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var item in enumerable)
                action(item);
        }

        /// <summary>
        /// Executes the specified action for each object async in the iteration.
        /// 
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="action">The action to be executed for each object</param>
        /// <returns>A task to wait for the operation</returns>
        public static async Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var item in enumerable)
                await action(item);
        }
    }
}
