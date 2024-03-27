namespace WouterVanRanst.Utils.Extensions
{
    public static class IEnumerableExtensions
    {
        // https://stackoverflow.com/a/5248390/1582323

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> sequence)
        {
            return sequence.Where(e => e != null);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence)
            where T : struct
        {
            return sequence.Where(e => e != null).Select(e => e.Value);
        }

        public static bool None<T>(this IEnumerable<T> sequence)
        {
            return !sequence.Any();
        }

        public static bool Multiple<TSource>(this IEnumerable<TSource> source)
        {
            // Analogous to Any() but with a check for multiple elements

            int count;
            return !source.TryGetNonEnumeratedCount<TSource>(out count) ? WithEnumerator(source) : count > 1;

#nullable disable
            static bool WithEnumerator(IEnumerable<TSource> source)
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                    return enumerator.MoveNext();
            }
        }
    }
}
