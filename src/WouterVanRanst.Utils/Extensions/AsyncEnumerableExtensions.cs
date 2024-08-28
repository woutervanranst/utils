namespace WouterVanRanst.Utils.Extensions;

public static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IEnumerable<Task<T>> source)
    {
        var results = await Task.WhenAll(source);
        return results.ToList();
    }
}