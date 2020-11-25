#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.Collections
{
    /// <summary>
    /// Extends <see cref="IAsyncEnumerable{T}"/> with functions not included in Interactive or the Framework
    /// </summary>
    public static class IAsyncEnumerableExtension
    {
        
        /// <summary>
        /// Executes the specified action for each object async in the iteration. 
        /// Ends the linq chain.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate</typeparam>
        /// <param name="enumerable">The enumeration for which the action is to be executed.</param>
        /// <param name="action">The action to be executed for each object</param>
        /// <returns>A task to wait for the operation</returns>
        public static async Task ForEach<T>(this IAsyncEnumerable<T> enumerable, Func<T, Task> action)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            await foreach (var item in enumerable)
                await action(item);
        }
    }
}
#endif