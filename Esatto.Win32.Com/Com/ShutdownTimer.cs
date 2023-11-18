#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    public class ShutdownTimer : IDisposable
    {
        private readonly TimeSpan Interval;
        private readonly Action Shutdown;
        private readonly SynchronizationContext? SyncCtx;
        private readonly Timer Timer;
        private bool isShutdown;
        private readonly object sync = new object();
        private readonly List<WeakReference> tracks = new List<WeakReference>();

        public ShutdownTimer(TimeSpan interval, Action shutdown, SynchronizationContext? syncCtx)
        {
            this.Interval = interval;
            this.Shutdown = shutdown;
            this.SyncCtx = syncCtx;
            this.Timer = new Timer(Tick, null, interval, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (isShutdown) return;
                isShutdown = true;
            }
            this.Timer.Dispose();
        }

        private void Tick(object? state)
        {
            bool end = false;
            try
            {
                lock (sync)
                {
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        var target = tracks[i].Target;
                        if (target is null 
                            || (target is IIsDead dead && dead.IsDead))
                        {
                            tracks.RemoveAt(i);
                            i--;
                        }
                    }

                    end = tracks.Count == 0;
                    if (end)
                    {
                        isShutdown = true;
                    }
                }

                if (end)
                {
                    Timer.Dispose();
                    if (SyncCtx is not null)
                    {
                        SyncCtx.Post(_ => Shutdown(), null);
                    }
                    else
                    {
                        Shutdown();
                    }
                }
            }
            finally
            {
                if (!end)
                {
                    Timer.Change(Interval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        public T Track<T>(T reference)
            where T : class
        {
            lock (sync)
            {
                if (isShutdown) throw new ObjectDisposedException(nameof(ShutdownTimer));

                tracks.Add(new WeakReference(reference));
            }
            return reference;
        }
    }

    public interface IIsDead
    {
        bool IsDead { get; }
    }
}
