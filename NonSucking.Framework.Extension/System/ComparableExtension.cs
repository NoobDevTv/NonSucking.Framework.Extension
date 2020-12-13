using System;

namespace NonSucking.Framework.Extension.System
{
    /// <summary>
    /// Extends the <see cref="IComparable"/> interface by more functions
    /// </summary>
    public static class ComparableExtension
    {
        /// <summary>
        /// Limits a number to be in between a given range
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val">The number to be limited</param>
        /// <param name="min">The inclusive minimum value</param>
        /// <param name="max">The inclusive maximum value</param>
        /// <returns>The limited number.
        /// If smaller than <paramref name="min"/> then <paramref name="min"/> will be returned.
        /// If greater than <paramref name="max"/> then <paramref name="max"/> will be returned.
        /// </returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
