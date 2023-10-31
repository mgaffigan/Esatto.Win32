using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Esatto.Utilities
{
    public class FilteredObservableCollection<TBaseItem, TNewItem> : IDisposable, IList, ICollection, IEnumerable, IList<TNewItem>, ICollection<TNewItem>, IEnumerable<TNewItem>, INotifyCollectionChanged, INotifyPropertyChanged, IObservableCollection<TNewItem>
        where TBaseItem : notnull
    {
        private readonly ObservableCollection<TNewItem> FilteredList;
        private readonly Dictionary<TBaseItem, TNewItem> TransformMap;
        private readonly Func<TBaseItem, bool> FilterPredicate;
        private readonly Func<TBaseItem, TNewItem> TransformFunc;
        private readonly Func<TNewItem, object>? SortKeySelector;
        private readonly IObservableCollection<TBaseItem> BaseList;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public FilteredObservableCollection(IObservableCollection<TBaseItem> underlyingList,
            Func<TBaseItem, bool>? filter = null,
            Func<TBaseItem, TNewItem>? transform = null,
            Func<TNewItem, object>? sortKeySelector = null)
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
            this.FilteredList = new ObservableCollection<TNewItem>();

            this.BaseList = underlyingList;
            this.FilterPredicate = filter ?? new Func<TBaseItem, bool>(_ => true);
            this.TransformFunc = transform ?? new Func<TBaseItem, TNewItem>(a => (TNewItem)(object)a);
            this.SortKeySelector = sortKeySelector;

            this.BaseList.CollectionChanged += this.OnUnderlyingList_CollectionChanged;
            ListReset();

            this.FilteredList.CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
            ((INotifyPropertyChanged)this.FilteredList).PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        public FilteredObservableCollection(IObservableCollection<TBaseItem> underlyingList, Func<TBaseItem, bool> filter)
            : this(underlyingList, filter, null, null)
        {
        }

        public FilteredObservableCollection(IObservableCollection<TBaseItem> underlyingList, Func<TBaseItem, TNewItem> transform)
            : this(underlyingList, null, transform, null)
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.BaseList.CollectionChanged -= this.OnUnderlyingList_CollectionChanged;
            ClearProjected();
        }

        private void OnUnderlyingList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset
                || e.Action == NotifyCollectionChangedAction.Move)
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
                FilteredList.Sort(SortKeySelector);
            }
        }

        private void ListReset()
        {
            ClearProjected();

            var newItems = BaseList.Where(FilterPredicate).Select(b => new { Original = b, NewItem = TransformFunc(b) });
            if (SortKeySelector != null)
            {
                newItems = newItems.OrderBy(a => SortKeySelector(a.NewItem));
            }

            foreach (var item in newItems)
            {
                this.FilteredList.Add(item.NewItem);
                this.TransformMap[item.Original] = item.NewItem;
            }
        }

        private void ClearProjected()
        {
            var removed = this.FilteredList.OfType<INotifyItemRemoved>().ToArray();
            this.FilteredList.Clear();
            this.TransformMap.Clear();
            foreach (var xfmd in removed)
            {
                xfmd.NotifyRemoved();
            }
        }

        public int Add(object? value)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public void Add(TNewItem? item)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public void Clear()
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public bool Contains(object? value) => this.Contains((TNewItem?)value);

        public bool Contains(TNewItem? item) => FilteredList.Contains(item!);

        public void CopyTo(Array array, int index) => ((IList)FilteredList).CopyTo(array, index);

        public void CopyTo(TNewItem[] array, int arrayIndex) => FilteredList.CopyTo(array, arrayIndex);

        public IEnumerator<TNewItem> GetEnumerator() => FilteredList.GetEnumerator();

        public int IndexOf(object? value) => this.IndexOf((TNewItem?)value);

        public int IndexOf(TNewItem? item) => FilteredList.IndexOf(item!);

        public void Insert(int index, object? value)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public void Insert(int index, TNewItem item)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public bool Remove(TNewItem item)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        public void Remove(object? value)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        void IList<TNewItem>.RemoveAt(int index)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        void IList.RemoveAt(int index)
        {
            throw new InvalidOperationException("FilteredObservableCollections are read-only");
        }

        IEnumerator IEnumerable.GetEnumerator() => FilteredList.GetEnumerator();

        public int Count => FilteredList.Count;

        public bool IsFixedSize => false;

        public bool IsReadOnly => true;

        public bool IsSynchronized => false;


        public TNewItem this[int index]
        {
            get
            {
                return this.FilteredList[index];
            }
            set
            {
                throw new InvalidOperationException("FilteredObservableCollections are read-only");
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((ICollection)this.FilteredList).SyncRoot;
            }
        }

        object? IList.this[int index]
        {
            get { return this[index]; }
            set { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }
        }
    }

    public static class FilteredObservableCollectionExtensions
    {
        public static FilteredObservableCollection<TItem, TItem> WhereObservable<TItem>(this IObservableCollection<TItem> @this, Func<TItem, bool> predicate)
            where TItem : notnull
        {
            return new FilteredObservableCollection<TItem, TItem>(@this, predicate);
        }

        public static FilteredObservableCollection<TItem, TNewItem> SelectObservable<TItem, TNewItem>(this IObservableCollection<TItem> @this, Func<TItem, TNewItem> transform)
            where TItem : notnull
        {
            return new FilteredObservableCollection<TItem, TNewItem>(@this, transform);
        }
    }
}
