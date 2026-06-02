#if NET

using Microsoft.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Win32.Time;

public static class WallClockDelay
{
    public static async Task DelayUntilUtcAsync(DateTime utcTarget, CancellationToken ct)
    {
        if (utcTarget.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("utcTarget must be a UTC DateTime", nameof(utcTarget));
        }

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var remaining = utcTarget - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            using var resumeInterrupt = new CancellationTokenSource();
            PowerModeChangedEventHandler onPowerModeChanged = (s, e) =>
            {
                if (e.Mode == PowerModes.Resume)
                {
                    resumeInterrupt.Cancel();
                }
            };

            SystemEvents.PowerModeChanged += onPowerModeChanged;
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, resumeInterrupt.Token);
                await Task.Delay(remaining, linked.Token);
                return;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Resume signal interrupted delay; recompute remaining wall-clock time.
            }
            finally
            {
                SystemEvents.PowerModeChanged -= onPowerModeChanged;
            }
        }
    }
}

#endif