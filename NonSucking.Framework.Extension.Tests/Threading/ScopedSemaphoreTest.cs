using NonSucking.Framework.Extension.Threading;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.Tests.Threading
{
    [TestFixture]
    [TestOf(typeof(ScopedSemaphore))]
    public sealed class ScopedSemaphoreTest
    {
        [TestCaseSource(typeof(ScopedSemaphoreTestCases), nameof(ScopedSemaphoreTestCases.CtorTestCases))]
        public void CtorTest(int? initalCount, int? maxCount, int count)
        {
            ScopedSemaphore semaphore = null;
            try
            {
                if (maxCount.HasValue && initalCount.HasValue)
                    semaphore = new ScopedSemaphore(initalCount.Value, maxCount.Value);
                else if (initalCount.HasValue)
                    semaphore = new ScopedSemaphore(initalCount.Value);
                else if (!initalCount.HasValue && !maxCount.HasValue)
                    semaphore = new ScopedSemaphore();
                else
                    throw new NotSupportedException();

                Assert.AreEqual(count, semaphore.CurrentCount);
            }
            finally
            {
                semaphore?.Dispose();
            }
        }

        [TestCaseSource(typeof(ScopedSemaphoreTestCases), nameof(ScopedSemaphoreTestCases.CtorExceptions))]
        public void CtorExceptionTest(Type type, TestDelegate exceptionDelegate)
            => Assert.Throws(type, exceptionDelegate);

        [Test]
        public async Task WaitTest()
        {
            using var semaphore = new ScopedSemaphore();
            using var manualReset = new ManualResetEvent(false);
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            bool flagOne = false, flagTwo = false;

            var task = Task.Run(() =>
            {
                using SemaphoreLock l = semaphore.Wait();

                Assert.IsTrue(l.HasEntered);
                flagOne = true;
                manualReset.WaitOne();
            });

            await Task.Delay(3);
            var task2 = Task.Run(() =>
            {
                using SemaphoreLock l = semaphore.Wait();
                if (tokenSource.Token.IsCancellationRequested)
                    return;
                flagTwo = true;
                Assert.IsFalse(l.HasEntered);
            });

            await Task.Delay(TimeSpan.FromSeconds(10));
            tokenSource.Cancel();
            manualReset.Set();
            await task;
            await task2;
            Assert.IsTrue(flagOne);
            Assert.IsFalse(flagTwo);

        }
    }
}
