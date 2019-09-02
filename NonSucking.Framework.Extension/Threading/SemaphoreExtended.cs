using System;
using System.Threading;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.Threading
{
    /// <summary>
    /// Represents a alternative to <see cref="SemaphoreSlim"/> that limits
    /// the number of threads that can access a resource or pool of resources concurrently.
    /// </summary>
    public sealed class SemaphoreExtended : IDisposable
    {
        /// <summary>
        /// Returns a <see cref="WaitHandle"/> that can be used to wait on the semaphore.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed</exception>
        public WaitHandle AvailableWaitHandle => internalSemaphore.AvailableWaitHandle;
        /// <summary>
        /// Gets the number of remaining threads that can enter the <see cref="SemaphoreExtended"/>
        /// </summary>
        public int CurrentCount => internalSemaphore.CurrentCount;

        private readonly SemaphoreSlim internalSemaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreExtended"/> class, specifying
        /// the initial number of requests that can be granted concurrently as 1.
        /// </summary>
        public SemaphoreExtended()
        {
            internalSemaphore = new SemaphoreSlim(1, 1);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreExtended"/> class, specifying
        /// the initial number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> is less than 0.</exception>
        public SemaphoreExtended(int initialCount)
        {
            internalSemaphore = new SemaphoreSlim(initialCount);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreExtended"/> class, specifying
        /// the initial and maximum number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently.</param>
        /// <param name="maxCount">The maximum number of requests for the semaphore that can be granted concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> is less than 0, 
        /// or <paramref name="initialCount"/> is greater than <paramref name="maxCount"/>, or <paramref name="maxCount"/> 
        /// is equal to or less than 0.</exception>
        public SemaphoreExtended(int initialCount, int maxCount)
        {
            internalSemaphore = new SemaphoreSlim(initialCount, maxCount);
        }

        /// <summary>
        /// Releases the <see cref="SemaphoreExtended"/> object once.
        /// </summary>
        /// <returns>The previous count of the <see cref="SemaphoreExtended"/></returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="SemaphoreFullException">The <see cref="SemaphoreExtended"/> has already reached its maximum size.</exception>
        internal int Release()
            => internalSemaphore.Release();
        /// <summary>
        /// Releases the <see cref="SemaphoreExtended"/> object a specified number of times.
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore.</param>
        /// <returns>The previous count of the <see cref="SemaphoreExtended"/></returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="SemaphoreFullException">The <see cref="SemaphoreExtended"/> has already reached its maximum size.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="releaseCount"/> is less than 1.</exception>
        internal int Release(int releaseCount)
            => internalSemaphore.Release(releaseCount);

        /// <summary>
        /// Blocks the current thread until it can enter the System.Threading.SemaphoreSlim.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed.</exception>
        public SemaphoreLock Wait()
        {
            internalSemaphore.Wait();
            return new SemaphoreLock(internalSemaphore, true);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreExtended"/>,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> token to observe.</param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">cancellationToken was canceled.</exception>
        /// <exception cref="ObjectDisposedException">The current instance has already been disposed. -or- The <see cref="CancellationTokenSource"/>
        /// that created <paramref name="cancellationToken"/> has already been disposed.</exception>
        public SemaphoreLock Wait(CancellationToken cancellationToken)
        {

            internalSemaphore.Wait(cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }
        /// <summary>
        /// Asynchronously waits to enter the System.Threading.SemaphoreSlim, using a 32-bit
        /// signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout"> The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely, or zero to test the state of the wait handle and return immediately.</param>
        /// <returns>A task that will complete with a result of true if the current thread successfully
        /// entered the System.Threading.SemaphoreSlim, otherwise with a result of false.</returns>
        public SemaphoreLock Wait(int millisecondsTimeout)
        {
            internalSemaphore.Wait(millisecondsTimeout);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public SemaphoreLock Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            internalSemaphore.Wait(millisecondsTimeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public SemaphoreLock Wait(TimeSpan timeout)
        {
            internalSemaphore.Wait(timeout);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public SemaphoreLock Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            internalSemaphore.Wait(timeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }

        public async Task<SemaphoreLock> WaitAsync()
        {
            await internalSemaphore.WaitAsync();
            return new SemaphoreLock(internalSemaphore, true);
        }
        public async Task<SemaphoreLock> WaitAsync(CancellationToken cancellationToken)
        {
            await internalSemaphore.WaitAsync(cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public async Task<SemaphoreLock> WaitAsync(int millisecondsTimeout)
        {
            await internalSemaphore.WaitAsync(millisecondsTimeout);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public async Task<SemaphoreLock> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            await internalSemaphore.WaitAsync(millisecondsTimeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public async Task<SemaphoreLock> WaitAsync(TimeSpan timeout)
        {
            await internalSemaphore.WaitAsync(timeout);
            return new SemaphoreLock(internalSemaphore, true);
        }
        public async Task<SemaphoreLock> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            await internalSemaphore.WaitAsync(timeout, cancellationToken);
            return new SemaphoreLock(internalSemaphore, true);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="SemaphoreExtended"/>
        /// </summary>
        public void Dispose()
            => internalSemaphore.Dispose();
    }
}
