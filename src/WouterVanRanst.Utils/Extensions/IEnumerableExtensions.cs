namespace WouterVanRanst.Utils.Extensions;

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

        return !source.TryGetNonEnumeratedCount<TSource>(out var count) ? WithEnumerator(source) : count > 1;

#nullable disable
        static bool WithEnumerator(IEnumerable<TSource> source)
        {
            using var enumerator = source.GetEnumerator();
            return enumerator.MoveNext();
        }
    }


    /// <summary>Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type.</summary>
    /// <param name="ordered">True to compare sequences in order; false to ignore the order.</param>
    public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, bool ordered)
    {
        if (first is null || second is null)
        {
            return false;
        }

        if (ordered)
        {
            return first.SequenceEqual(second);
        }
        else
        {
            return first.OrderBy(x => x).SequenceEqual(second.OrderBy(x => x));
        }
    }


    /// <summary>
    /// Returns a sequence of elements from the input collection that have duplicates based on the specified key selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <typeparam name="TKey">The type of the key used for comparison.</typeparam>
    /// <param name="collection">The input collection.</param>
    /// <param name="keySelector">A function to extract the key for comparison.</param>
    /// <returns>A sequence of elements that have duplicates based on the specified key selector.</returns>
    public static IEnumerable<T> DuplicatesBy<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(collection, nameof(collection));

        return collection
            .GroupBy(keySelector)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group);
    }
}