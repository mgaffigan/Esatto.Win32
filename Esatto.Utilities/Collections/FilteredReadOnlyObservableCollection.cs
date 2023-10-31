using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Esatto.Utilities
{
    public class FilteredReadOnlyObservableCollection<TBaseItem, TNewItem>
        : FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, object>
        where TBaseItem : notnull
    {
        public FilteredReadOnlyObservableCollection(ReadOnlyObservableCollection<TBaseItem> underlyingList,
            Func<TBaseItem, bool>? filter = null, Func<TBaseItem, TNewItem>? transform = null,
            Func<TNewItem, object>? sortKeySelector = null, IComparer<object>? sortKeyComparer = null)
            : base(underlyingList, filter, transform, sortKeySelector, sortKeyComparer)
        {
        }

        public FilteredReadOnlyObservableCollection(ReadOnlyObservableCollection<TBaseItem> underlyingList, Func<TBaseItem, bool> filter)
            : base(underlyingList, filter, null, null)
        {
        }

        public FilteredReadOnlyObservableCollection(ReadOnlyObservableCollection<TBaseItem> underlyingList, Func<TBaseItem, TNewItem> transform)
            : base(underlyingList, null, transform, null)
        {
        }

    }

    public class FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, TSortKey> : ReadOnlyObservableCollection<TNewItem>
        where TBaseItem : notnull
    {
        private readonly ObservableCollection<TNewItem> FilteredList;
        private readonly Dictionary<TBaseItem, TNewItem> TransformMap;
        private readonly Func<TBaseItem, bool> FilterPredicate;
        private readonly Func<TBaseItem, TNewItem> TransformFunc;
        private readonly Func<TNewItem, TSortKey>? SortKeySelector;
        private readonly ReadOnlyObservableCollection<TBaseItem> BaseList;
        private readonly IComparer<TSortKey>? SortKeyComparer;
        private readonly object DeferredSortActionKey = new object();

        [Obsolete("Class inherits ReadOnlyObservableCollection")]
        public ReadOnlyObservableCollection<TNewItem> Projection { get; }

        private FilteredReadOnlyObservableCollection(ObservableCollection<TNewItem> projectedList,
            ReadOnlyObservableCollection<TBaseItem> underlyingList,
            Func<TBaseItem, bool>? filter, Func<TBaseItem, TNewItem>? transform,
            Func<TNewItem, TSortKey>? sortKeySelector, IComparer<TSortKey>? sortKeyComparer)
            : base(projectedList)
        {
            if (underlyingList == null)
            {
                throw new ArgumentNullException(nameof(underlyingList), "Contract assertion not met: underlyingList != null");
            }
            if (!(filter != null || transform != null || sortKeySelector != null))
            {
                throw new ArgumentException("Contract assertion not met: filter != null || transform != null || sortKeySelector != null", nameof(filter));
            }

            this.TransformMap = new Dictionary<TBaseItem, TNewItem>();
            this.FilteredList = projectedList;
#pragma warning disable CS0618 // Type or member is obsolete
            this.Projection = this;
#pragma warning restore CS0618 // Type or member is obsolete

            this.BaseList = underlyingList;
            this.FilterPredicate = filter ?? new Func<TBaseItem, bool>(_ => true);
            this.TransformFunc = transform ?? new Func<TBaseItem, TNewItem>(a => (TNewItem)(object)a);
            this.SortKeySelector = sortKeySelector;
            this.SortKeyComparer = sortKeyComparer;

            _ = new WeakIntermediate(this, this.BaseList);
            ListReset();
        }

        public FilteredReadOnlyObservableCollection(ReadOnlyObservableCollection<TBaseItem> underlyingList,
            Func<TBaseItem, bool>? filter = null, Func<TBaseItem, TNewItem>? transform = null,
            Func<TNewItem, TSortKey>? sortKeySelector = null, IComparer<TSortKey>? sortKeyComparer = null)
            : this(new ObservableCollection<TNewItem>(), underlyingList, filter, transform, sortKeySelector, sortKeyComparer)
        {
        }

        private sealed class WeakIntermediate
        {
            private readonly WeakReference<FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, TSortKey>> WeakReference;

            public WeakIntermediate(FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, TSortKey> parent, INotifyCollectionChanged collection)
            {
                this.WeakReference = new WeakReference<FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, TSortKey>>(parent);
                collection.CollectionChanged += Collection_CollectionChanged;
            }

            private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (!WeakReference.TryGetTarget(out var proj))
                {
                    var baseCollection = (INotifyCollectionChanged)sender!;
                    baseCollection.CollectionChanged -= Collection_CollectionChanged;
                    return;
                }

                proj.OnUnderlyingList_CollectionChanged(sender!, e);
            }
        }

        public FilteredReadOnlyObservableCollection(ReadOnlyObservableCollection<TBaseItem> underlyingList, Func<TBaseItem, bool> filter)
            : this(underlyingList, filter, null, null)
        {
        }

        public FilteredReadOnlyObservableCollection(ReadOnlyObservableCollection<TBaseItem> underlyingList, Func<TBaseItem, TNewItem> transform)
            : this(underlyingList, null, transform, null)
        {
        }

        private void OnUnderlyingList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                // no-op, only the final transform can sort.
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ListReset();
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (TBaseItem oldItem in e.OldItems!)
                {
                    if (TransformMap.TryGetValue(oldItem, out var xfmd))
                    {
                        TransformMap.Remove(oldItem);
                        FilteredList.Remove(xfmd);

                        var newItemIRemovable = xfmd as INotifyItemRemoved;
                        if (newItemIRemovable != null)
                        {
                            newItemIRemovable.NotifyRemoved();
                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (TBaseItem newItem in e.NewItems!)
                {
                    if (FilterPredicate(newItem))
                    {
                        var xfmd = TransformFunc(newItem);

                        TransformMap[newItem] = xfmd;
                        FilteredList.Add(xfmd);
                    }
                }
            }

            if (SortKeySelector != null)
            {
                ObservableCollectionUpdateScope.RunDeferred(DeferredSortActionKey, () =>
                {
                    FilteredList.Sort(SortKeySelector, SortKeyComparer);
                });
            }
        }

        public void NotifyCriteriaChanged() => ListReset();
        public void NotifyCriteriaChanged(TBaseItem baseItem)
        {
            bool wasShown = TransformMap.TryGetValue(baseItem, out var xfmd);
            var sbShown = FilterPredicate(baseItem);

            if (wasShown == sbShown)
            {
                if (SortKeySelector != null && sbShown)
                {
                    ObservableCollectionUpdateScope.RunDeferred(DeferredSortActionKey, () =>
                    {
                        FilteredList.Sort(SortKeySelector, SortKeyComparer);
                    });
                }

                return;
            }

            if (sbShown)
            {
                OnUnderlyingList_CollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, baseItem));
            }
            else
            {
                OnUnderlyingList_CollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, baseItem));
            }
        }

        private void ListReset()
        {
            this.FilteredList.Clear();
            this.TransformMap.Clear();

            var newItems = BaseList.Where(FilterPredicate).Select(b => new { Original = b, NewItem = TransformFunc(b) });
            if (SortKeySelector != null)
            {
                newItems = newItems.OrderBy(a => SortKeySelector(a.NewItem), SortKeyComparer);
            }

            foreach (var item in newItems)
            {
                this.FilteredList.Add(item.NewItem);
                this.TransformMap[item.Original] = item.NewItem;
            }
        }
    }

    internal sealed class SelectManyReadOnlyObservableCollectionProjection<TItem, TNewItem> : ReadOnlyObservableCollection<TNewItem>
        where TItem : notnull
    {
        private readonly Dictionary<TItem, ReadOnlyObservableCollection<TNewItem>> SourceCollectionMapping;
        private readonly ReadOnlyObservableCollection<TItem> SourceCollection;
        private readonly Func<TItem, ReadOnlyObservableCollection<TNewItem>> Enumerator;
        private readonly ObservableCollection<TNewItem> TransformedItems;

        [Obsolete("Class inherits ReadOnlyObservableCollection")]
        public ReadOnlyObservableCollection<TNewItem> Projection { get; }

        public SelectManyReadOnlyObservableCollectionProjection(ReadOnlyObservableCollection<TItem> sourceCollection, Func<TItem, ReadOnlyObservableCollection<TNewItem>> enumerator)
            : this(new ObservableCollection<TNewItem>(), sourceCollection, enumerator)
        {
            if (sourceCollection == null)
            {
                throw new ArgumentNullException(nameof(sourceCollection), "Contract assertion not met: sourceCollection != null");
            }
            if (enumerator == null)
            {
                throw new ArgumentNullException(nameof(enumerator), "Contract assertion not met: enumerator != null");
            }
        }

        private SelectManyReadOnlyObservableCollectionProjection(ObservableCollection<TNewItem> projectedList,
            ReadOnlyObservableCollection<TItem> sourceCollection, Func<TItem, ReadOnlyObservableCollection<TNewItem>> enumerator)
            : base(projectedList)
        {
            this.SourceCollectionMapping = new Dictionary<TItem, ReadOnlyObservableCollection<TNewItem>>(ReferenceEqualityComparer<TItem>.Instance);
            this.TransformedItems = projectedList;
#pragma warning disable CS0618 // Type or member is obsolete
            this.Projection = this;
#pragma warning restore CS0618 // Type or member is obsolete

            this.SourceCollection = sourceCollection;
            this.Enumerator = enumerator;

            ((INotifyCollectionChanged)sourceCollection).CollectionChanged += Source_CollectionChagned;
            foreach (var collection in sourceCollection)
            {
                Source_CollectionChagned(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (object)collection));
            }
        }

        private void Source_CollectionChagned(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var newItem = (TItem)e.NewItems![0]!;
                        var collection = Enumerator(newItem);
                        this.SourceCollectionMapping[newItem] = collection;

                        ((INotifyCollectionChanged)collection).CollectionChanged += TItem_CollectionChanged;
                        foreach (var item in collection)
                        {
                            TransformedItems.Add(item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        var oldItem = (TItem)e.OldItems![0]!;
                        var collection = SourceCollectionMapping[oldItem];
                        ((INotifyCollectionChanged)collection).CollectionChanged -= TItem_CollectionChanged;
                        foreach (var item in collection)
                        {
                            TransformedItems.Remove(item);
                        }
                        this.SourceCollectionMapping.Remove(oldItem);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        Source_CollectionChagned(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                        Source_CollectionChagned(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    {
                        // no-op, position invariant
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    {
                        TransformedItems.Clear();
                        SourceCollectionMapping.Clear();
                    }
                    break;

                default: throw new NotSupportedException();
            }
        }

        private void TItem_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var newItem = (TNewItem)e.NewItems![0]!;
                        TransformedItems.Add(newItem);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        var oldItem = (TNewItem)e.OldItems![0]!;
                        TransformedItems.Remove(oldItem);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        TItem_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                        TItem_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    {
                        // no-op, position invariant
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    {
                        TransformedItems.Clear();

                        foreach (var collection in SourceCollectionMapping.Values)
                        {
                            foreach (var item in collection)
                            {
                                TransformedItems.Add(item);
                            }
                        }
                    }
                    break;

                default: throw new NotSupportedException();
            }
        }

        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        {
            internal static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

            private ReferenceEqualityComparer() { }

            public bool Equals(T? x, T? y)
            {
                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }

    public static class FilteredReadOnlyObservableCollectionExtensions
    {
        public static ReadOnlyObservableCollection<TItem> AsReadOnly<TItem>(this ObservableCollection<TItem> @this)
        {
            return new ReadOnlyObservableCollection<TItem>(@this);
        }

        public static ReadOnlyObservableCollection<TItem> WhereObservable<TItem>(this ReadOnlyObservableCollection<TItem> @this, Func<TItem, bool> predicate)
            where TItem : notnull
        {
            return new FilteredReadOnlyObservableCollection<TItem, TItem>(@this, predicate);
        }

        public static ReadOnlyObservableCollection<TNewItem> SelectObservable<TItem, TNewItem>(this ReadOnlyObservableCollection<TItem> @this, Func<TItem, TNewItem> transform)
            where TItem : notnull
        {
            return new FilteredReadOnlyObservableCollection<TItem, TNewItem>(@this, transform);
        }

        public static FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, TSortKey> Transform<TBaseItem, TNewItem, TSortKey>(
            this ReadOnlyObservableCollection<TBaseItem> @this,
            Func<TBaseItem, bool>? filter = null, Func<TBaseItem, TNewItem>? transform = null, Func<TNewItem, TSortKey>? sortKeySelector = null, IComparer<TSortKey>? comparer = null)
            where TBaseItem : notnull
        {
            return new FilteredReadOnlyObservableCollection<TBaseItem, TNewItem, TSortKey>(@this, filter, transform, sortKeySelector, comparer);
        }

        public static FilteredReadOnlyObservableCollection<TBaseItem, TNewItem> Transform<TBaseItem, TNewItem>(
            this ReadOnlyObservableCollection<TBaseItem> @this,
            Func<TBaseItem, bool>? filter = null, Func<TBaseItem, TNewItem>? transform = null)
            where TBaseItem : notnull
        {
            return new FilteredReadOnlyObservableCollection<TBaseItem, TNewItem>(@this, filter, transform);
        }

        public static ReadOnlyObservableCollection<TNewItem> SelectManyObservable<TItem, TNewItem>(this ReadOnlyObservableCollection<TItem> @this, Func<TItem, ReadOnlyObservableCollection<TNewItem>> transform)
            where TItem : notnull
        {
            return new SelectManyReadOnlyObservableCollectionProjection<TItem, TNewItem>(@this, transform);
        }

        public static ReadOnlyObservableCollection<TItem> SelectManyObservable<TItem>(this ReadOnlyObservableCollection<ReadOnlyObservableCollection<TItem>> @this)
            where TItem : notnull
        {
            return new SelectManyReadOnlyObservableCollectionProjection<ReadOnlyObservableCollection<TItem>, TItem>(@this, a => a);
        }

        public static ReadOnlyObservableCollection<TItem> SelectManyObservable<TItem>(this IEnumerable<ReadOnlyObservableCollection<TItem>> @this)
            where TItem : notnull
        {
            var items = new ObservableCollection<ReadOnlyObservableCollection<TItem>>(@this);
            return new SelectManyReadOnlyObservableCollectionProjection<ReadOnlyObservableCollection<TItem>, TItem>(items.AsReadOnly(), a => a);
        }
    }

    public sealed class ObservableCollectionUpdateScope : IDisposable
    {
        private static AsyncLocal<ObservableCollectionUpdateScope?> _Current = new AsyncLocal<ObservableCollectionUpdateScope?>();
        internal static ObservableCollectionUpdateScope? Current
        {
            get { return _Current.Value; }
            set { _Current.Value = value; }
        }

        public static IDisposable OpenUpdateScope()
        {
            if (Current != null)
            {
                throw new InvalidOperationException("ObservableCollectionUpdateScope already open");
            }

            var newItem = new ObservableCollectionUpdateScope();
            Current = newItem;
            return newItem;
        }

        private Dictionary<object, Action> DeferredUpdates = new Dictionary<object, Action>();

        public ObservableCollectionUpdateScope()
        {
        }

        public static void RunDeferred(object key, Action action)
        {
            var context = Current;
            if (context == null)
            {
                action();
            }
            else
            {
                context.AddDeferredAction(key, action);
            }
        }

        public void AddDeferredAction(object key, Action action)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "Contract assertion not met: key != null");
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Contract assertion not met: action != null");
            }
            if (!(Current == this))
            {
                throw new ArgumentException(nameof(Current));
            }

            this.DeferredUpdates[key] = action;
        }

        public void Dispose()
        {
            if (Current != this)
            {
                throw new InvalidOperationException();
            }

            try
            {
                foreach (var deferred in DeferredUpdates.Values)
                {
                    deferred();
                }
            }
            finally
            {
                Current = null;
            }
        }
    }
}
