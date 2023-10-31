using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Utilities
{
    public class ProjectedObservableCollection<T> : IObservableCollection<T>, INotifyPropertyChanged
        where T : notnull
    {
        private readonly Func<IReadOnlyList<T>> Projection;
        private readonly ObservableCollectionImpl<T> Collection;

        public ProjectedObservableCollection(Func<IReadOnlyList<T>> proj)
        {
            this.Projection = proj ?? throw new ArgumentNullException(nameof(proj));
            this.Collection = new ObservableCollectionImpl<T>();
            this.Collection.CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
            ((INotifyPropertyChanged)this.Collection).PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        public void Invalidate() => Collection.MakeEqualTo(Projection());

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        #region IObservableCollection<T>

        public T this[int index] => Collection[index];
        public int Count => Collection.Count;
        public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Collection.GetEnumerator();

        #endregion
    }

    public sealed class ProjectedObservableCollection<TSource, TResult> : ProjectedObservableCollection<TResult>, IDisposableObservableCollection<TResult>
        where TResult : notnull
    {
        private HashSet<INotifyCollectionChanged> attached = new();
        private readonly IObservableCollection<TSource> Source;
        private readonly Func<TSource, IObservableCollection<TResult>> Selector;

        public ProjectedObservableCollection(IObservableCollection<TSource> source,
            Func<TSource, IObservableCollection<TResult>> selector)
            : base(() => source.SelectMany(s => selector(s)).ToList())
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
            var result = new ProjectedObservableCollection<T>(func);
            result.Invalidate();
            return result;
        }

        public static ProjectedObservableCollection<T> AsInvalidatingObservable<T>(this IEnumerable<T> source)
            where T : notnull
        {
            var result = new ProjectedObservableCollection<T>(() => source.ToList());
            result.Invalidate();
            return result;
        }

        public static IDisposableObservableCollection<TResult> SelectManyObservable<TSource, TResult>(this IObservableCollection<TSource> source,
            Func<TSource, IObservableCollection<TResult>> selector)
            where TResult : notnull
            where TSource : notnull
        {
            var result = new ProjectedObservableCollection<TSource, TResult>(source, selector);
            result.Invalidate();
            return result;
        }
    }
}
