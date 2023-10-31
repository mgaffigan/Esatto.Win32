namespace Esatto.Utilities
{
    public static class EnumerableExtensions
    {
        public static TSource? MinOrDefault<TSource>(this IEnumerable<TSource> source)
            where TSource : struct
        {
            return source.Cast<TSource?>().Min();
        }

        public static TSource? MaxOrDefault<TSource>(this IEnumerable<TSource> source)
            where TSource : struct
        {
            return source.Cast<TSource?>().Max();
        }

        public static TSource? SingleOrDefault<TSource, TException>(this IEnumerable<TSource> source)
            where TException : Exception, new()
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default;
            }
            var result = enumerator.Current;
            if (enumerator.MoveNext())
            {
                throw new TException();
            }
            return result;
        }

        public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<Nullable<TSource>> source)
            where TSource : struct
            => source.Where(n => n.HasValue).Select(n => n!.Value);

        public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
            => source.Where(n => n is not null).Cast<TSource>();

        public static IEnumerable<IEnumerable<TItem>> Batch<TItem>(this IEnumerable<TItem> @this, int batchSize)
        {
            if (!(batchSize > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Contract assertion not met: batchSize > 0");
            }
            if (@this == null)
            {
                throw new ArgumentNullException(nameof(@this), "Contract assertion not met: @this != null");
            }

            var batch = new List<TItem>(batchSize);
            foreach (var item in @this)
            {
                batch.Add(item);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        public static int? IndexOfOrDefault<T>(this IList<T> list, Predicate<T> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }

            return null;
        }
    }
}
