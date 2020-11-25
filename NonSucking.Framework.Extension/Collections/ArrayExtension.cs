using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.Collections
{
    /// <summary>
    /// Extends the basic type <see cref="Array"/> with practical functions that are otherwise only static.
    /// </summary>
    public static class ArrayExtension
    {
        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array to search.</param>
        /// <param name="value">The object to locate in array.</param>
        /// <returns> The index of the first occurrence of value in array, if found; otherwise, the lower bound of the array minus 1.</returns>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="RankException">array is multidimensional.</exception>
        public static int IndexOf(this Array array, object value)
            => Array.IndexOf(array, value);

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array to search.</param>
        /// <param name="value">The object to locate in array.</param>
        /// <returns> The index of the first occurrence of value in array, if found; otherwise, the lower bound of the array minus 1.</returns>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="RankException">array is multidimensional.</exception>
        public static int IndexOf(this Array array, object value, int startIndex)
            => Array.IndexOf(array, value, startIndex);

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array to search.</param>
        /// <param name="value">The object to locate in array.</param>
        /// <returns> The index of the first occurrence of value in array, if found; otherwise, the lower bound of the array minus 1.</returns>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="RankException">array is multidimensional.</exception>
        public static int IndexOf(this Array array, object value, int startIndex, int count)
            => Array.IndexOf(array, value, startIndex, count);

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array to search.</param>
        /// <param name="value">The object to locate in array.</param>
        /// <returns> The index of the first occurrence of value in array, if found; otherwise, the lower bound of the array minus 1.</returns>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="RankException">array is multidimensional.</exception>
        public static int IndexOf<T>(this T[] array, T value)
            => Array.IndexOf(array, value);

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array to search.</param>
        /// <param name="value">The object to locate in array.</param>
        /// <returns> The index of the first occurrence of value in array, if found; otherwise, the lower bound of the array minus 1.</returns>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="RankException">array is multidimensional.</exception>
        public static int IndexOf<T>(this T[] array, T value, int startIndex)
            => Array.IndexOf(array, value, startIndex);

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array to search.</param>
        /// <param name="value">The object to locate in array.</param>
        /// <returns> The index of the first occurrence of value in array, if found; otherwise, the lower bound of the array minus 1.</returns>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="RankException">array is multidimensional.</exception>
        public static int IndexOf<T>(this T[] array, T value, int startIndex, int count)
            => Array.IndexOf(array, value, startIndex, count);
    }
}
