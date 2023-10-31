using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Utilities
{
    public static class TaskExtensions
    {
        public static async Task AsTask(this CancellationToken ct)
        {
            // https://stackoverflow.com/questions/18670111/task-from-cancellation-token
            var tcs = new TaskCompletionSource<bool>();
            using var _ = ct.Register(() => tcs.TrySetCanceled(ct), useSynchronizationContext: false);
            await tcs.Task;
        }
    }
}
