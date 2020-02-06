using System;
using System.Threading;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.Threading
{
    /// <summary>
    /// Represents a alternative to <see cref="SemaphoreSlim"/> that limits
    /// the number of threads that can access a resource or pool of resources concurrently.
    /// </summary>
    public sealed class ScopedSemaphore : IDisposable
    {
        /// <summary>
        /// Returns a <see cref="WaitHandle"/> that can be used to wait on the semaphore.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed</exception>
        public WaitHandle AvailableWaitHandle => internalSemaphore.AvailableWaitHandle;
        /// <summary>
        /// Gets the number of remaining threads that can enter the <see cref="ScopedSemaphore"/>
        /// </summary>
        public int CurrentCount => internalSemaphore.CurrentCount;

        private readonly SemaphoreSlim internalSemaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedSemaphore"/> class, specifying
        /// the initial number of requests that can be granted concurrently as 1.
        /// </summary>
        public ScopedSemaphore()
        {
            internalSemaphore = new SemaphoreSlim(1, 1);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedSemaphore"/> class, specifying
        /// the initial number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> is less than 0.</exception>
        public ScopedSemaphore(int initialCount)
        {
            internalSemaphore = new SemaphoreSlim(initialCount);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedSemaphore"/> class, specifying
        /// the initial and maximum number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently.</param>
        /// <param name="maxCount">The maximum number of requests for the semaphore that can be granted concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> is less than 0, 
        /// or <paramref name="initialCount"/> is greater than <paramref name="maxCount"/>, or <paramref name="maxCount"/> 
        /// is equal to or less than 0.</exception>
        public ScopedSemaphore(int initialCount, int maxCount)
        {
            internalSemaphore = new SemaphoreSlim(initialCount, maxCount);
        }

        /// <summary>
        /// Releases the <see cref="ScopedSemaphore"/> object once.
        /// </summary>
        /// <returns>The previous count of the <see cref="ScopedSemaphore"/></returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="SemaphoreFullException">The <see cref="ScopedSemaphore"/> has already reached its maximum size.</exception>
        internal int Release()
            => internalSemaphore.Release();
        /// <summary>
        /// Releases the <see cref="ScopedSemaphore"/> object a specified number of times.
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore.</param>
        /// <returns>The previous count of the <see cref="ScopedSemaphore"/></returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="SemaphoreFullException">The <see cref="ScopedSemaphore"/> has already reached its maximum size.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="releaseCount"/> is less than 1.</exception>
        internal int Release(int releaseCount)
            => internalSemaphore.Release(releaseCount);

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="ScopedSemaphore"/>.
        /// </summary>
        /// <returns>A <see cref="SemaphoreLock"/> object that monitors the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        public SemaphoreLock Wait()
        {
            internalSemaphore.Wait();
            return new SemaphoreLock(internalSemaphore, true);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="ScopedSemaphore"/>,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> token to observe.</param>
        /// <returns>A <see cref="SemaphoreLock"/> object that monitors the blocking process.</returns>
        /// <exception cref="OperationCanceledException">cancellationToken was canceled.</exception>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        public SemaphoreLock Wait(CancellationToken cancellationToken)
        {
            internalSemaphore.Wait(cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="ScopedSemaphore"/>, using a 32-bit
        /// signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout"> The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely, or zero to test the state of the wait handle and return immediately.</param>
        /// <returns>A <see cref="SemaphoreLock"/> object that monitors the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public SemaphoreLock Wait(int millisecondsTimeout)
        {
            var entered = internalSemaphore.Wait(millisecondsTimeout);
            return new SemaphoreLock(internalSemaphore, entered);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="ScopedSemaphore"/>,
        /// using a 32-bit signed integer that specifies the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout"> The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely, or zero to test the state of the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A <see cref="SemaphoreLock"/> object that monitors the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        /// <exception cref = "OperationCanceledException" > cancellationToken was canceled.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public SemaphoreLock Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var entered = internalSemaphore.Wait(millisecondsTimeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, entered);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="ScopedSemaphore"/>,
        /// using a <see cref="TimeSpan"/> to specify the timeout.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        ///  that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents 0 milliseconds 
        ///  to test the wait handle and return immediately.</param>
        /// <returns>A <see cref="SemaphoreLock"/> object that monitors the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public SemaphoreLock Wait(TimeSpan timeout)
        {
            var entered = internalSemaphore.Wait(timeout);
            return new SemaphoreLock(internalSemaphore, entered);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="ScopedSemaphore"/>,
        /// using a <see cref="TimeSpan"/> to specify the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        ///  that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents 0 milliseconds 
        ///  to test the wait handle and return immediately.</param>
        ///  <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A <see cref="SemaphoreLock"/> object that monitors the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        /// <exception cref="OperationCanceledException">cancellationToken was canceled.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public SemaphoreLock Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var entered = internalSemaphore.Wait(timeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, entered);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="ScopedSemaphore"/>.
        /// </summary>
        /// <returns>A task that will complete with a result of an <see cref="SemaphoreLock"/> object that monitors 
        /// the blocking process.</returns>
        public async Task<SemaphoreLock> WaitAsync()
        {
            await internalSemaphore.WaitAsync();
            return new SemaphoreLock(internalSemaphore, true);
        }
        /// <summary>
        /// Asynchronously waits to enter the <see cref="ScopedSemaphore"/>,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A task that will complete with a result of an <see cref="SemaphoreLock"/> object that monitors 
        /// the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        /// <exception cref="OperationCanceledException">cancellationToken was canceled.</exception>
        public async Task<SemaphoreLock> WaitAsync(CancellationToken cancellationToken)
        {
            await internalSemaphore.WaitAsync(cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }
        /// <summary>
        /// Asynchronously waits to enter the <see cref="ScopedSemaphore"/>,
        /// using a 32-bit signed integer that specifies the timeout.
        /// </summary>
        /// <param name="millisecondsTimeout"> The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely, or zero to test the state of the wait handle and return immediately.</param>
        /// <returns>A task that will complete with a result of an <see cref="SemaphoreLock"/> object that monitors 
        /// the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public async Task<SemaphoreLock> WaitAsync(int millisecondsTimeout)
        {
            var entered = await internalSemaphore.WaitAsync(millisecondsTimeout);
            return new SemaphoreLock(internalSemaphore, entered);
        }
        /// <summary>
        /// Asynchronously waits to enter the <see cref="ScopedSemaphore"/>,
        /// using a 32-bit signed integer that specifies the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout"> The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely, or zero to test the state of the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A task that will complete with a result of an <see cref="SemaphoreLock"/> object that monitors 
        /// the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        /// <exception cref="OperationCanceledException">cancellationToken was canceled.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public async Task<SemaphoreLock> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var entered = await internalSemaphore.WaitAsync(millisecondsTimeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, entered);
        }
        /// <summary>
        /// Asynchronously waits to enter the <see cref="ScopedSemaphore"/>,
        /// using a <see cref="TimeSpan"/> to specify the timeout.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        ///  that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents 0 milliseconds 
        ///  to test the wait handle and return immediately.</param>
        /// <returns>A task that will complete with a result of an <see cref="SemaphoreLock"/> object that monitors 
        /// the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public async Task<SemaphoreLock> WaitAsync(TimeSpan timeout)
        {
            var entered = await internalSemaphore.WaitAsync(timeout);
            return new SemaphoreLock(internalSemaphore, entered);
        }
        /// <summary>
        /// Asynchronously waits to enter the <see cref="ScopedSemaphore"/>,
        /// using a <see cref="TimeSpan"/> to specify the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        ///  that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents 0 milliseconds 
        ///  to test the wait handle and return immediately.</param>
        ///  <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A task that will complete with a result of an <see cref="SemaphoreLock"/> object that monitors 
        /// the blocking process.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        /// <exception cref="OperationCanceledException">cancellationToken was canceled.</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout is a negative number other than -1, which represents an infinite timeout
        ///  -or- timeout is greater than <see cref="int.MaxValue"/></exception>
        public async Task<SemaphoreLock> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var entered = await internalSemaphore.WaitAsync(timeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, entered);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="ScopedSemaphore"/>
        /// </summary>
        public void Dispose()
            => internalSemaphore.Dispose();
    }
}
