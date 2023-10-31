using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Utilities
{
    // CA1001 is false alarm: SemaphoreSlim does not have resources to dispose unless `AvailableWaitHandle` is accessed
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public sealed class AsyncMutex
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly SemaphoreSlim sem = new(1, 1);

        public async Task<IDisposable> AcquireAsync(CancellationToken ct = default)
        {
            await sem.WaitAsync(ct);
            return new DelegateDisposable(() => sem.Release());
        }

        /// <summary>
        /// Acquire the mutex without waiting or throw <see cref="TimeoutException"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public async Task<IDisposable> AcquireImmediateAsync()
        {
            if (!await sem.WaitAsync(0))
            {
                throw new TimeoutException("Reentrance is not permitted");
            }

            return new DelegateDisposable(() => sem.Release());
        }
    }
}
