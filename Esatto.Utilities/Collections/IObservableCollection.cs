using System.Collections.Specialized;

namespace Esatto.Utilities
{
    /// <summary>
    /// Interface which allows the replacement of ObservableCollection
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public interface IObservableCollection<out TElement> : INotifyCollectionChanged, IReadOnlyList<TElement>
    {
    }

    public interface IDisposableObservableCollection<out TElement> : IObservableCollection<TElement>, IDisposable
    {
    }
}
