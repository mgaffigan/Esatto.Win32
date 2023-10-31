using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Esatto.Utilities
{
    /// <summary>
    /// Implementation of <see cref="IObservableCollection{TElement}"/>
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name")]
    public class ObservableCollectionImpl<TElement> : ObservableCollection<TElement>, IObservableCollection<TElement>
    {
        public ObservableCollectionImpl()
        {
        }

        public ObservableCollectionImpl(IEnumerable<TElement> contents)
            : base(contents)
        {
        }

        public ObservableCollectionImpl(List<TElement> contents)
            : base(contents)
        {
        }
    }
}
