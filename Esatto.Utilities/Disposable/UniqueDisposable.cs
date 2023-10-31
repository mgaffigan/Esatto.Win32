using System;

namespace Esatto.Utilities
{
    public static class DisposableExtensions
    {
        public static UniqueDisposable<T> MakeUnique<T>(this T disposable)
            where T : class, IDisposable
            => new UniqueDisposable<T>(disposable);

#if NET
        public static UniqueAsyncDisposable<T> MakeUniqueAsync<T>(this T disposable)
            where T : class, IAsyncDisposable
            => new UniqueAsyncDisposable<T>(disposable);
#endif

        public static void DisposeAll(this IEnumerable<IDisposable> disposables)
        {
            var exceptions = new List<Exception>();
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            exceptions.ThrowIfNotEmpty();
        }

        public static async ValueTask DisposeAllAsync(this IEnumerable<IAsyncDisposable> disposables)
        {
            var exceptions = new List<Exception>();
            foreach (var disposable in disposables)
            {
                try
                {
                    await disposable.DisposeAsync();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            exceptions.ThrowIfNotEmpty();
        }
    }

    public sealed class UniqueDisposable<T> : IDisposable
        where T : class, IDisposable
    {
        private T? Disposable;
        public T Value => Disposable ?? throw new ObjectDisposedException(nameof(UniqueDisposable<T>));

        public UniqueDisposable(T disposable)
        {
            this.Disposable = disposable;
        }

        public T Take()
        {
            var t = Disposable ?? throw new ObjectDisposedException(nameof(UniqueDisposable<T>));
            Disposable = null;
            return t;
        }

        public void Dispose() => Disposable?.Dispose();

        public static implicit operator T(UniqueDisposable<T> t) => t.Value;
    }

#if NET
    public sealed class UniqueAsyncDisposable<T> : IAsyncDisposable
        where T : class, IAsyncDisposable
    {
        private T? Disposable;
        public T Value => Disposable ?? throw new ObjectDisposedException(nameof(UniqueAsyncDisposable<T>));

        public UniqueAsyncDisposable(T disposable)
        {
            this.Disposable = disposable;
        }

        public T Take()
        {
            var t = Disposable ?? throw new ObjectDisposedException(nameof(UniqueAsyncDisposable<T>));
            Disposable = null;
            return t;
        }

        public ValueTask DisposeAsync() => Disposable?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
#endif
}
