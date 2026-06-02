using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Esatto.Utilities
{
    public class ProjectedObservableCollection<T> : IObservableCollection<T>, INotifyPropertyChanged
        where T : notnull
    {
        private readonly Func<IReadOnlyList<T>> Projection;
        private readonly ObservableCollectionImpl<T>? Collection;
        private IReadOnlyList<T> List;

        [Obsolete("Specify ProjectionChangedMode")]
        public ProjectedObservableCollection(Func<IReadOnlyList<T>> proj)
            : this(proj, ProjectionChangedMode.Reset)
        {
            // nop
        }

        public ProjectedObservableCollection(Func<IReadOnlyList<T>> proj, ProjectionChangedMode mode)
        {
            this.Projection = proj ?? throw new ArgumentNullException(nameof(proj));

            if (mode == ProjectionChangedMode.Incremental)
            {
                this.List = this.Collection = new ObservableCollectionImpl<T>();
                this.Collection.CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
                ((INotifyPropertyChanged)this.Collection).PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
            }
            else
            {
                this.List = proj();
            }
        }

        public void Invalidate()
        {
            if (Collection is not null)
            {
                Collection.MakeEqualTo(Projection());
            }
            else
            {
                var newList = Projection();
                if (List.SequenceEqual(newList)) return;
                List = newList;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        #region IObservableCollection<T>

        public T this[int index] => List[index];
        public int Count => List.Count;
        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

        #endregion
    }

    public enum ProjectionChangedMode
    {
        Reset,
        Incremental,
    }

    public sealed class ProjectedObservableCollection<TSource, TResult> 
        : ProjectedObservableCollection<TResult>, IDisposableObservableCollection<TResult>
        where TResult : notnull
    {
        private HashSet<INotifyCollectionChanged> attached = new();
        private readonly IObservableCollection<TSource> Source;
        private readonly Func<TSource, IObservableCollection<TResult>> Selector;

        [Obsolete("Specify ProjectionChangedMode")]
        public ProjectedObservableCollection(IObservableCollection<TSource> source,
            Func<TSource, IObservableCollection<TResult>> selector)
            : this(source, selector, ProjectionChangedMode.Reset)
        {
            // nop
        }

        public ProjectedObservableCollection(IObservableCollection<TSource> source,
            Func<TSource, IObservableCollection<TResult>> selector, ProjectionChangedMode mode)
            : base(() => source.SelectMany(s => selector(s)).ToList(), mode)
        {
            this.Source = source;
            this.Selector = selector;

            source.CollectionChanged += Source_CollectionChanged;
            Source_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Dispose()
        {
            Source.CollectionChanged -= Source_CollectionChanged;
            DetachAll();
        }

        private void DetachAll()
        {
            foreach (var former in attached)
            {
                former.CollectionChanged -= Child_CollectionChanged;
            }
            attached.Clear();
        }

        private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IObservableCollection<TResult> item in e.NewItems!)
                    {
                        item.CollectionChanged += Child_CollectionChanged;
                        attached.Add(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (IObservableCollection<TResult> item in e.OldItems!)
                    {
                        item.CollectionChanged -= Child_CollectionChanged;
                        attached.Remove(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (IObservableCollection<TResult> item in e.OldItems!)
                    {
                        item.CollectionChanged -= Child_CollectionChanged;
                        attached.Remove(item);
                    }
                    foreach (IObservableCollection<TResult> item in e.NewItems!)
                    {
                        item.CollectionChanged += Child_CollectionChanged;
                        attached.Add(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    // nop
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        DetachAll();

                        foreach (var item in Source.Select(Selector))
                        {
                            item.CollectionChanged += Child_CollectionChanged;
                            attached.Add(item);
                        }
                    }
                    break;
            }
            Invalidate();
        }

        private void Child_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Invalidate();
        }
    }

    public static class ProjectedObservableCollectionExtensions
    {
        public static ProjectedObservableCollection<T> AsInvalidatingObservable<T>(this Func<IReadOnlyList<T>> func)
            where T : notnull
        {
            return new ProjectedObservableCollection<T>(func, ProjectionChangedMode.Reset);
        }

        public static ProjectedObservableCollection<T> AsInvalidatingObservable<T>(this IEnumerable<T> source)
            where T : notnull
        {
            return new ProjectedObservableCollection<T>(() => source.ToList(), ProjectionChangedMode.Reset);
        }

        public static IDisposableObservableCollection<TResult> SelectManyObservable<TSource, TResult>(this IObservableCollection<TSource> source,
            Func<TSource, IObservableCollection<TResult>> selector)
            where TResult : notnull
            where TSource : notnull
        {
            return new ProjectedObservableCollection<TSource, TResult>(source, selector, ProjectionChangedMode.Reset);
        }
    }
}
