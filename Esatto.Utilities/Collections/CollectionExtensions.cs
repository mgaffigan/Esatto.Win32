using System;
using System.Collections.ObjectModel;

namespace Esatto.Utilities
{
    public static class CollectionExtensions
    {
        //check Esatto.Wpf.CustomControls for more (PresentationFramework dependency)
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> addCollection)
        {
            foreach (T obj in addCollection)
                collection.Add(obj);
        }

        public static void RemoveRange<T>(this ObservableCollection<T> collection, IEnumerable<T> remove)
        {
            foreach (T obj in remove.ToArray())
                collection.Remove(obj);
        }

        public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> remove)
        {
            foreach (T obj in remove.ToArray())
                collection.Remove(obj);
        }

        public static void RemoveAll<T>(this IList<T> @this, Predicate<T> filter)
        {
            for (int i = 0; i < @this.Count;)
            {
                if (filter(@this[i]))
                {
                    @this.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public static void Sort<TSource, TKey>(this ObservableCollection<TSource> collection,
            Func<TSource, TKey> keySelector, IComparer<TKey>? comparer = null)
        {
            var sorted = collection.OrderBy(keySelector, comparer).ToList();
            int moves = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (!object.ReferenceEquals(collection[i], sorted[i]))
                {
                    moves++;
                    collection.Move(collection.IndexOf(sorted[i]), i);
                }
                else
                {
                    // no-op, already in position
                }
            }
        }

        /// <summary>
        /// Make one list match another, with minimal changes in target for single item adds, replaces, and deletes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="source"></param>
        public static void MakeEqualTo<T>(this IList<T> target, IReadOnlyList<T> source, IEqualityComparer<T>? comparer = null)
            where T : notnull
        {
            comparer ??= EqualityComparer<T>.Default;

            int i = 0;
            for (; i < source.Count; i++)
            {
                var nextS = source.Count > i + 1 ? source[i + 1] : default;
                target.MakeIndexEqualTo(i, source[i], nextS, comparer);
            }

            // remove excess
            while (i < target.Count)
            {
                (target[target.Count - 1] as INotifyItemRemoved)?.NotifyRemoved();
                target.RemoveAt(target.Count - 1);
            }

#if DEBUG
            if (!target.SequenceEqual(source, comparer))
            {
                throw new InvalidOperationException("Assertion failed in MakeEqualTo");
            }
#endif
        }

        private static void MakeIndexEqualTo<T>(this IList<T> target, int i, T value, T? nextS, IEqualityComparer<T> comparer)
            where T : notnull
        {
            // target list is shorter than index
            if (i >= target.Count)
            {
                target.Add(value);
                return;
            }

            // there are items in the target
            var ct = target[i];
            if (comparer.Equals(ct, value))
            {
                // nop
                return;
            }

            // they're different
            var nextT = target.Count > i + 1 ? target[i + 1] : default;
            // if nextT and nextS are the same, replacement
            if (comparer.Equals(nextT, nextS))
            {
                (target[i] as INotifyItemRemoved)?.NotifyRemoved();
                target[i] = value;
                return;
            }

            // if nextT and value are equal, removal
            if (comparer.Equals(nextT, value))
            {
                (target[i] as INotifyItemRemoved)?.NotifyRemoved();
                target.RemoveAt(i);
                return;
            }

            // if nextS and ct are equal, insert
            target.Insert(i, value);
        }
    }
}
