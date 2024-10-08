﻿namespace WouterVanRanst.Utils.Extensions;

public static class IGroupingExtensions
{
    public static IEnumerable<TElement> GetGroup<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> groupings, TKey groupId)
    {
        var group = groupings.SingleOrDefault(g => g.Key.Equals(groupId));
        return group?.AsEnumerable() ?? Enumerable.Empty<TElement>();
    }
}