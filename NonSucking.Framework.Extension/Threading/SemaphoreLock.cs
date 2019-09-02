using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NonSucking.Framework.Extension.Threading
{
    public struct SemaphoreLock : IDisposable
    {
        public static SemaphoreLock Empty => new SemaphoreLock(null, false);

        public bool HasEntered { get; }

        private readonly SemaphoreSlim internalSemaphore;

        public SemaphoreLock(SemaphoreSlim semaphore, bool hasEntered)
        {
            internalSemaphore = semaphore;
            HasEntered = hasEntered;
        }

        public void Dispose()
        {
            if (HasEntered)
                internalSemaphore?.Release();
        }
    }
}
