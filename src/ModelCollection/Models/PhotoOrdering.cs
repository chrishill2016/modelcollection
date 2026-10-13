namespace ModelCollection.Models;

/// <summary>Applies a car's chosen photo order (cover first) to its photos.</summary>
public static class PhotoOrdering
{
    /// <summary>Photos in the car's chosen order, then any not yet ordered, oldest first.</summary>
    public static IReadOnlyList<CarPhoto> Sort(IEnumerable<CarPhoto> photos, IReadOnlyList<string> order)
    {
        var rank = order.Select((id, index) => (id, index)).GroupBy(x => x.id).ToDictionary(g => g.Key, g => g.First().index);
        return photos
            .OrderBy(p => rank.TryGetValue(p.Id, out var r) ? r : int.MaxValue)
            .ThenBy(p => p.Created)
            .ToList();
    }

    /// <summary>The order with <paramref name="photoId"/> moved to the front.</summary>
    public static IReadOnlyList<string> MakeCover(IReadOnlyList<CarPhoto> sorted, string photoId) =>
        sorted.Select(p => p.Id).Where(id => id != photoId).Prepend(photoId).ToList();

    /// <summary>The order with <paramref name="photoId"/> moved one place left (-1) or right (+1).</summary>
    public static IReadOnlyList<string> Move(IReadOnlyList<CarPhoto> sorted, string photoId, int direction)
    {
        var ids = sorted.Select(p => p.Id).ToList();
        var index = ids.IndexOf(photoId);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ids.Count)
        {
            return ids;
        }

        (ids[index], ids[target]) = (ids[target], ids[index]);
        return ids;
    }
}
