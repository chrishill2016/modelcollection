using ModelCollection.Models;

namespace ModelCollection.Features.Stats;

public sealed record StatBucket(string Label, int Count);

/// <summary>Headline numbers and breakdowns for the stats page.</summary>
public sealed record CollectionStats(
    int TotalCars,
    int BoxedCars,
    decimal TotalSpent,
    decimal TotalEstimatedValue,
    int CarsWithValue,
    IReadOnlyList<StatBucket> ByManufacturer,
    IReadOnlyList<StatBucket> ByScale,
    IReadOnlyList<StatBucket> ByMake,
    IReadOnlyList<StatBucket> BySeries,
    int LimitedEditions,
    IReadOnlyList<StatBucket> ByDecade)
{
    /// <summary>Top N buckets per breakdown; the rest fold into "Other".</summary>
    public const int MaxBuckets = 8;

    public static CollectionStats From(IReadOnlyCollection<ModelCar> cars) => new(
        cars.Count,
        cars.Count(c => c.HasBox),
        cars.Sum(c => c.PurchasePrice ?? 0),
        cars.Sum(c => c.EstimatedValue ?? 0),
        cars.Count(c => c.EstimatedValue is not null),
        Breakdown(cars.Select(c => c.Manufacturer)),
        Breakdown(cars.Select(c => c.Scale)),
        Breakdown(cars.Select(c => c.Make)),
        Breakdown(cars.Where(c => !string.IsNullOrWhiteSpace(c.Series)).Select(c => c.Series)),
        cars.Count(c => c.IsLimitedEdition),
        DecadeBreakdown(cars));

    internal static IReadOnlyList<StatBucket> Breakdown(IEnumerable<string> values)
    {
        var groups = values
            .Select(v => string.IsNullOrWhiteSpace(v) ? "Unknown" : v.Trim())
            .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatBucket(g.First(), g.Count()))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Label)
            .ToList();

        if (groups.Count <= MaxBuckets)
        {
            return groups;
        }

        var top = groups.Take(MaxBuckets - 1).ToList();
        top.Add(new StatBucket("Other", groups.Skip(MaxBuckets - 1).Sum(b => b.Count)));
        return top;
    }

    // Decades stay in chronological order rather than by count.
    private static IReadOnlyList<StatBucket> DecadeBreakdown(IEnumerable<ModelCar> cars) =>
        cars.GroupBy(c => c.Year is { } y ? y / 10 * 10 : (int?)null)
            .OrderBy(g => g.Key ?? int.MaxValue)
            .Select(g => new StatBucket(g.Key is { } d ? $"{d}s" : "Unknown", g.Count()))
            .ToList();
}
