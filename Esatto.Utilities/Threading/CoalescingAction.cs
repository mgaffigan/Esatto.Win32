namespace Esatto.Utilities
{
    public sealed class CoalescingAction : IDisposable
    {
        private Action Action;
        private TimeSpan CoalescePeriod;
        private TimeSpan MaximumLatency;

        private readonly object syncProcessing = new object();
        private readonly Timer tmrCoalesce;

        private DateTime dueTime;
        private DateTime maxDueTime;

        /// <summary>
        /// Indicates whether the worker thread is trying to clear isSet
        /// </summary>
        private bool inProgress;

        /// <summary>
        /// Indicates whether there is an outstanding request to run
        /// </summary>
        private bool isSet;

        public bool IsSet => isSet;
        public bool IsSetOrRunning => isSet | inProgress;

        public CoalescingAction(Action action, TimeSpan coalescingPeriod,
            TimeSpan maximumLatency)
        {
            if (coalescingPeriod < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(coalescingPeriod));
            }
            if (maximumLatency < coalescingPeriod && maximumLatency != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLatency));
            }

            this.Action = action ?? throw new ArgumentNullException(nameof(action));
            this.CoalescePeriod = coalescingPeriod;
            this.MaximumLatency = maximumLatency;

            this.tmrCoalesce = new Timer(tmrCoalesce_Tick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Set()
        {
            lock (syncProcessing)
            {
                var now = DateTime.UtcNow;
                var nextRun = now + CoalescePeriod;

                if (maxDueTime == default && MaximumLatency != Timeout.InfiniteTimeSpan)
                {
                    maxDueTime = now + MaximumLatency;
                }

                if (nextRun > maxDueTime && maxDueTime != default)
                {
                    nextRun = maxDueTime;
                }

                this.dueTime = nextRun;
                var timeTillNextRun = nextRun - now;

                // schedule
                // if we spent too much time waiting for lock to the point where we went
                // past the maxDueTime, timeTillNextRun may be negative
                if (timeTillNextRun <= TimeSpan.Zero)
                {
                    timeTillNextRun = TimeSpan.Zero;
                }

                // schedule the timer
                isSet = true;
                if (inProgress)
                {
                    // no-op, already running
                }
                else
                {
                    tmrCoalesce.Change(timeTillNextRun, Timeout.InfiniteTimeSpan);
                }
            }
        }

        public void Flush()
        {
            lock (syncProcessing)
            {
                // wait for complete
                if (inProgress)
                {
                    while (inProgress)
                    {
                        Monitor.Wait(syncProcessing);
                    }
                    return;
                }

                // run in this call stack
                dueTime = DateTime.MinValue;
            }
            tmrCoalesce_Tick(null);
        }

        private void tmrCoalesce_Tick(object? o)
        {
            lock (syncProcessing)
            {
                // avoid reentrance (from flush or otherwise)
                if (inProgress)
                {
                    return;
                }

                // no run is needed (spurious tick due to some reason I can't think of)
                if (!isSet)
                {
                    return;
                }

                // if the tick event was delivered before a call to Timer.Change in Set
                // we may be prematurely activating
                var now = DateTime.UtcNow;
                if (dueTime > now + TimeSpan.FromMilliseconds(30))
                {
                    // dueTime > now + 30ms => dueTime - now > 30ms
                    tmrCoalesce.Change(dueTime - now, Timeout.InfiniteTimeSpan);
                    return;
                }

                // we move to in progress
                inProgress = true;
                isSet = false;
            }

            // we loop until we are able to unset InProcessing without an intervening
            // set coming in
            bool keepGoing = true;
            while (keepGoing)
            {
                try
                {
                    // we are on a worker thread, so we can call immediately
                    Action();
                }
                finally
                {
                    lock (syncProcessing)
                    {
                        // reset the due times so the next call will start a new max
                        // latency period
                        maxDueTime = dueTime = default(DateTime);

                        if (isSet)
                        {
                            // we go again
                            isSet = false;
                        }
                        else
                        {
                            // we are no longer running.  Any call to set will generate a new
                            // tick event
                            keepGoing = false;
                            inProgress = false;

                            // notify callers of Flush
                            Monitor.PulseAll(syncProcessing);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            using (var mreDone = new ManualResetEvent(false))
            {
                if (tmrCoalesce.Dispose(mreDone))
                {
                    mreDone.WaitOne();
                }
            }
        }
    }

    public sealed class ContextAwareCoalescingAction : IDisposable
    {
        private readonly CoalescingAction Action;

        public ContextAwareCoalescingAction(Action action, TimeSpan coalescingPeriod,
            TimeSpan maximumLatency, SynchronizationContext context)
        {
            this.Action = new CoalescingAction(() =>
            {
                context.Post(_ => action(), null);
            }, coalescingPeriod, maximumLatency);
        }

        public void Set() => Action.Set();
        // Flush on background thread since Action may be calling trying to reenter the context
        public Task FlushAsync() => Task.Run(Action.Flush);

        public void Dispose() => Action.Dispose();
    }
}