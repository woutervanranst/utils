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
    }
}
