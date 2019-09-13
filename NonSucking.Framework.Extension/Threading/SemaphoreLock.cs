using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NonSucking.Framework.Extension.Threading
{
    /// <summary>
    /// An object that monitors the blocking process of a semaphore. 
    /// By disposing this object the semaphore is released.
    /// </summary>
    public struct SemaphoreLock : IDisposable
    {
        /// <summary>
        /// Returns an object without <see cref="SemaphoreSlim"/> and <see cref="HasEntered"/> false.
        /// </summary>
        public static SemaphoreLock Empty => new SemaphoreLock(null, false);

        /// <summary>
        /// Indicates whether the <see cref="SemaphoreSlim"/> for this operation was entered or not.
        /// </summary>
        public bool HasEntered { get; }

        private readonly SemaphoreSlim internalSemaphore;

        /// <summary>
        /// Creates a new montioring object for a sepcific <see cref="SemaphoreSlim"/>
        /// </summary>
        /// <param name="semaphore">The semaphore whose process is to be monitored.</param>
        /// <param name="hasEntered">Indicates whether the <see cref="SemaphoreSlim"/> 
        /// for this operation was entered or not.</param>
        public SemaphoreLock(SemaphoreSlim semaphore, bool hasEntered)
        {
            internalSemaphore = semaphore;
            HasEntered = hasEntered;
        }

        /// <summary>
        /// Releases this process again for the assigned semaphore.
        /// </summary>
        public void Dispose()
        {
            if (HasEntered)
                internalSemaphore?.Release();
        }
    }
}
