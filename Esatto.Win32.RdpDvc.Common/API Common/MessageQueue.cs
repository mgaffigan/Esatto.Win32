using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.ClientPluginApi
{
    // Helper class to convert event based Reads to async reads
    internal sealed class MessageQueue : IDisposable
    {
        private readonly object syncReceive = new object();
        private readonly List<byte[]> QueuedMessages = new List<byte[]>();
        private readonly List<TaskCompletionSource<byte[]>> ReceiveThunks = new List<TaskCompletionSource<byte[]>>();
        private bool isDisposed;

        public void Dispose()
        {
            AssertAlive();
            isDisposed = true;

            TaskCompletionSource<byte[]>[] tcss;
            lock (syncReceive)
            {
                tcss = ReceiveThunks.ToArray();
                ReceiveThunks.Clear();
            }

            foreach (var n in tcss)
            {
                n.SetCanceled();
            }
        }

        private void AssertAlive()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(MessageQueue));
            }
        }

        public Task<byte[]> ReadAsync(CancellationToken ct)
        {
            AssertAlive();

            // Check if there is a queued result, otherwise queue a callback
            TaskCompletionSource<byte[]> tcs;
            lock (syncReceive)
            {
                if (QueuedMessages.Count > 0)
                {
                    var result = QueuedMessages[0];
                    QueuedMessages.RemoveAt(0);
                    return Task.FromResult(result);
                }

                tcs = new TaskCompletionSource<byte[]>();
                ReceiveThunks.Add(tcs);
            }
            ct.Register(() =>
            {
                lock (syncReceive)
                {
                    if (!ReceiveThunks.Remove(tcs))
                    {
                        // no longer queued, ignore cancellation
                        return;
                    }
                }

                // May call back to client code, run outside lock
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }

        public void WriteMessage(byte[] data)
        {
            AssertAlive();

            TaskCompletionSource<byte[]>? tcs = null;
            lock (syncReceive)
            {
                if (ReceiveThunks.Count > 0)
                {
                    tcs = ReceiveThunks[0];
                    ReceiveThunks.RemoveAt(0);
                }
                else
                {
                    QueuedMessages.Add(data);
                }
            }

            // This may call back to client code - run outside lock
            tcs?.SetResult(data);
        }
    }
}
