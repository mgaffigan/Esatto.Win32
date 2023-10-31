using System.Collections.Specialized;
using System.Collections;

namespace Esatto.Utilities
{
    public class ObservableCollectionProxy<T> : IObservableCollection<T>
    {
        private readonly IReadOnlyList<T> ThisEnumerable;
        private readonly INotifyCollectionChanged ThisNotifyCollection;

        public ObservableCollectionProxy(INotifyCollectionChanged collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), "Contract assertion not met: collection != null");
            }

            this.ThisEnumerable = (IReadOnlyList<T>)collection;
            this.ThisNotifyCollection = collection;
            this.ThisNotifyCollection.CollectionChanged += (sender, e) => CollectionChanged?.Invoke(sender, e);
        }

        public T this[int index] => ThisEnumerable[index];

        public int Count => ThisEnumerable.Count;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public IEnumerator<T> GetEnumerator()
        {
            return ThisEnumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ThisEnumerable.GetEnumerator();
        }
    }
}