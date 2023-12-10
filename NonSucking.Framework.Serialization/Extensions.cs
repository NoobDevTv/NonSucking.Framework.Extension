using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NonSucking.Framework.Serialization;

public static class Extensions
{
    public static T? FirstOrNull<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        where T : struct
    {
        foreach (var v in enumerable)
        {
            if (predicate(v))
                return v;
        }
        return null;
    }
}