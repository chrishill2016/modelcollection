using ModelCollection.Features.Stats;

namespace ModelCollection.Tests.Features;

public class CollectionStatsTests
{
    [Fact]
    public void Totals_and_breakdowns()
    {
        ModelCar[] cars =
        [
            TestData.Car("Ferrari") with { Manufacturer = "Corgi", HasBox = true, PurchasePrice = 10, EstimatedValue = 50, Year = 1962 },
            TestData.Car("Ford") with { Manufacturer = "corgi", PurchasePrice = 5, Year = 1966 },
            TestData.Car("Porsche") with { Manufacturer = "Minichamps", Year = 1973 },
            TestData.Car("Mini") with { Manufacturer = "", Year = null },
        ];

        var stats = CollectionStats.From(cars);

        Assert.Equal(4, stats.TotalCars);
        Assert.Equal(1, stats.BoxedCars);
        Assert.Equal(15, stats.TotalSpent);
        Assert.Equal(50, stats.TotalEstimatedValue);
        Assert.Equal(1, stats.CarsWithValue);
        Assert.Equal(new StatBucket("Corgi", 2), stats.ByManufacturer[0]);
        Assert.Contains(new StatBucket("Unknown", 1), stats.ByManufacturer);
        Assert.Equal(["1960s", "1970s", "Unknown"], stats.ByDecade.Select(b => b.Label));
        Assert.Equal([2, 1, 1], stats.ByDecade.Select(b => b.Count));
    }

    [Fact]
    public void Long_tails_fold_into_other()
    {
        var values = Enumerable.Range(1, 12).Select(i => $"Brand {i:00}").Append("Brand 01");

        var buckets = CollectionStats.Breakdown(values);

        Assert.Equal(CollectionStats.MaxBuckets, buckets.Count);
        Assert.Equal(new StatBucket("Brand 01", 2), buckets[0]);
        Assert.Equal(new StatBucket("Other", 5), buckets[^1]);
        Assert.Equal(13, buckets.Sum(b => b.Count));
    }

    [Fact]
    public void Empty_collection() => Assert.Equal(0, CollectionStats.From([]).TotalCars);
}
