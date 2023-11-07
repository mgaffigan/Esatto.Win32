using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Utilities.Disposable
{
    public sealed class AggregateDisposable : IDisposable
    {
        private readonly IDisposable[] Disposables;

        public AggregateDisposable(params IDisposable[] disposables)
        {
            this.Disposables = disposables;
        }

        public void Dispose() => Disposables.DisposeAll();
    }

    public sealed class AggregateAsyncDisposable : IAsyncDisposable
    {
        private readonly IAsyncDisposable[] Disposables;

        public AggregateAsyncDisposable(params IAsyncDisposable[] disposables)
        {
            this.Disposables = disposables;
        }

        public ValueTask DisposeAsync() => Disposables.DisposeAllAsync();
    }
}
