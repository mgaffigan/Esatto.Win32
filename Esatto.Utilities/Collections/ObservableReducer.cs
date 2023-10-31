using System.Collections.Specialized;

namespace Esatto.Utilities
{
    public sealed class ObservableReducer<TSource, TResult> : IObservable<TResult>, IDisposable
        where TSource : class, INotifyCollectionChanged
    {
        private List<IObserver<TResult>> observers = new();
        private TResult Value;
        private TSource? Source;
        private readonly Func<TSource, TResult> Reducer;

        public ObservableReducer(TSource source, Func<TSource, TResult> reducer)
        {
            this.Value = reducer(source);
            this.Source = source;
            this.Reducer = reducer;
            source.CollectionChanged += Source_CollectionChanged;
        }

        public void Dispose()
        {
            if (this.Source is null) return;
            this.Source.CollectionChanged -= Source_CollectionChanged;
            this.Source = null;

            foreach (var o in GetObservers())
            {
                o.OnCompleted();
            }
        }

        public void Invalidate()
        {
            Source_CollectionChanged(null, null);
        }

        private IObserver<TResult>[] GetObservers() { lock (observers) { return observers.ToArray(); } }

        private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs? e)
        {
            if (Source is null) return;

            TResult result;
            try
            {
                result = Reducer(Source);
            }
            catch (Exception ex)
            {
                foreach (var o in GetObservers())
                {
                    o.OnError(ex);
                }
                return;
            }

            foreach (var o in GetObservers())
            {
                o.OnNext(result);
            }
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            if (Source is null) throw new ObjectDisposedException(nameof(ObservableReducer<TSource, TResult>));

            lock (observers)
            {
                observers.Add(observer);
            }

            observer.OnNext(Value);

            return new DelegateDisposable(() =>
            {
                lock (observers)
                {
                    observers.Remove(observer);
                }
            });
        }
    }

    public static class ReducerObservableCollectionExtensions
    {
        public static ObservableReducer<TSource, TResult> ReduceObservable<TSource, TResult>(this TSource source, Func<TSource, TResult> reduce)
            where TSource : class, INotifyCollectionChanged
        {
            return new ObservableReducer<TSource, TResult>(source, reduce);
        }
    }
}
