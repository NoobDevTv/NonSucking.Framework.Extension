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
        /// Executes the specified action for each object in the iteration.
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="func">The function to execute for each item and pass the current Index. 
        /// Return <see langword="false"/> to break the loop</param>
        public static void For<T>(this IEnumerable<T> enumerable, Func<T, int, bool> func)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            var index = 0;
            foreach (T item in enumerable)
            {
                if (!func(item, index))
                    return;

                ++index;
            }
        }

        /// <summary>
        /// Executes the specified action for each object async in the iteration.
        /// 
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="func">The action to be executed for each object</param>
        /// <returns>A task to wait for the operation</returns>
        public static async Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> func)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (T item in enumerable)
                await func(item);
        }

        /// <summary>
        /// Executes the specified action for each object async in the iteration.
        /// 
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="func">The action to be executed for each object. Pass the item and the index of the item</param>
        /// <returns>A task to wait for the operation</returns>
        public static async Task ForAsync<T>(this IEnumerable<T> enumerable, Func<T, int, Task> func)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            var index = 0;
            foreach (T item in enumerable)
            {
                await func(item, index);
                ++index;
            }
        }

        /// <summary>
        /// Executes the specified action for each object async in the iteration.
        /// 
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="func">The function to execute for each item and pass the current Index. 
        /// Return <see langword="false"/> to break the loop</param>
        /// <returns>A task to wait for the operation</returns>
        public static async Task ForAsync<T>(this IEnumerable<T> enumerable, Func<T, int, Task<bool>> func)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            var index = 0;
            foreach (T item in enumerable)
            {
                if (!await func(item, index))
                    return;

                ++index;
            }
        }
    }
}
