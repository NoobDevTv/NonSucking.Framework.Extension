using NonSucking.Framework.Extension.Threading;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.Tests.Threading
{
    public static class ScopedSemaphoreTestCases
    {
        internal static IEnumerable CtorTestCases()
        {
            yield return new TestCaseData(null, null, 1)
                        .SetName("Default Ctor initalization");

            yield return new TestCaseData(1, null, 1)
                        .SetName("Default Ctor initalization init 1");

            yield return new TestCaseData(0, null, 0)
                        .SetName("Default Ctor initalization init 0");

            yield return new TestCaseData(1, 1, 1)
                        .SetName("Default Ctor initalization init 1 and max 1");

            yield return new TestCaseData(3, 3, 3)
                        .SetName("Default Ctor initalization init 3 and max 3");

            yield return new TestCaseData(1, 3, 1)
                        .SetName("Default Ctor initalization init 1 and max 3");
        }

        internal static IEnumerable CtorExceptions()
        {
            yield return new TestCaseData(typeof(ArgumentOutOfRangeException), new TestDelegate(() =>
            {
                using var semphore = new ScopedSemaphore(-20);
            }))
            .SetName($"Throw {nameof(ArgumentOutOfRangeException)} when init count is lass then 0");

            yield return new TestCaseData(typeof(ArgumentOutOfRangeException), new TestDelegate(() =>
            {
                using var semphore = new ScopedSemaphore(-20, 7);
            }))
            .SetName($"Throw {nameof(ArgumentOutOfRangeException)} when init count is lass then 0, max count 7");

            yield return new TestCaseData(typeof(ArgumentOutOfRangeException), new TestDelegate(() =>
            {
                using var semphore = new ScopedSemaphore(20, 10);
            }))
            .SetName($"Throw {nameof(ArgumentOutOfRangeException)} when init count is grather then max count 10");
        }
    }
}
