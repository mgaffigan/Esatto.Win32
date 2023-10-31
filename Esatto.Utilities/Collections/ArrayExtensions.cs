namespace Esatto.Utilities
{
    public static class ArrayExtensions
    {
        public static void Fill<T>(this IList<T> src, T value)
        {
            for (int i = 0; i < src.Count; i++)
            {
                src[i] = value;
            }
        }

        public static void CopyTo<T>(this IReadOnlyList<T> src, IList<T> destination) => CopyTo(src, 0, destination, 0, src.Count);
        public static void CopyTo<T>(this IReadOnlyList<T> src, int srcOffset, IList<T> destination) => CopyTo(src, srcOffset, destination, 0, src.Count);
        public static void CopyTo<T>(this IReadOnlyList<T> src, int srcOffset, IList<T> destination, int dstOffset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                destination[dstOffset + i] = src[srcOffset + i];
            }
        }
    }
}
