namespace WouterVanRanst.Utils.Extensions;

public static class ObjectExtensions
{
    public static T[] AsArray<T>(this T element)
    {
        return new T[] { element };
    }
}