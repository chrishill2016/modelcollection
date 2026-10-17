using ModelCollection.Features.Collection;

namespace ModelCollection.Tests.Features;

public class CollectionQueryTests
{
    private static readonly ModelCar[] Cars =
    [
        TestData.Car("Ferrari", "250 GTO", "1") with { Manufacturer = "Corgi", Scale = "1:43", HasBox = true, Year = 1962, Tags = ["Le Mans"] },
        TestData.Car("Porsche", "911", "2") with { Manufacturer = "Minichamps", Scale = "1:18", Year = 1973, Colour = "Silver", EstimatedValue = 80 },
        TestData.Car("Ford", "GT40", "3") with { Manufacturer = "corgi", Scale = "1:43", Year = 1966, Condition = CarCondition.Mint, EstimatedValue = 120 },
    ];

    private static string[] Ids(CollectionQuery q) => q.Apply(Cars).Select(c => c.Id).ToArray();

    [Fact]
    public void No_filters_sorts_by_make_and_model() => Assert.Equal(["1", "3", "2"], Ids(new CollectionQuery()));

    [Theory]
    [InlineData("porsche", new[] { "2" })]
    [InlineData("silver 911", new[] { "2" })]
    [InlineData("le mans", new[] { "1" })]
    [InlineData("1966", new[] { "3" })]
    [InlineData("nothing", new string[0])]
    public void Search_matches_all_terms_across_fields(string search, string[] expected) =>
        Assert.Equal(expected, Ids(new CollectionQuery { Search = search }));

    [Fact]
    public void Brand_filter_ignores_case() =>
        Assert.Equal(["1", "3"], Ids(new CollectionQuery { Manufacturer = "Corgi" }));

    [Fact]
    public void Filters_combine() =>
        Assert.Equal(["3"], Ids(new CollectionQuery { Scale = "1:43", Condition = CarCondition.Mint }));

    [Fact]
    public void Boxed_filter() =>
        Assert.Equal(["2", "3"], Ids(new CollectionQuery { Boxed = false, Sort = CarSort.Year }).OrderBy(x => x).ToArray());

    [Fact]
    public void Sort_by_value_puts_most_valuable_first() =>
        Assert.Equal(["3", "2", "1"], Ids(new CollectionQuery { Sort = CarSort.Value }));

    [Fact]
    public void Sort_by_year() => Assert.Equal(["1", "3", "2"], Ids(new CollectionQuery { Sort = CarSort.Year }));

    [Fact]
    public void HasFilters_ignores_sort()
    {
        Assert.False(new CollectionQuery { Sort = CarSort.Value }.HasFilters);
        Assert.True(new CollectionQuery { Boxed = true }.HasFilters);
    }

    [Fact]
    public void Needs_details_filter_finds_cars_without_brand_or_scale()
    {
        ModelCar[] cars = [.. Cars, TestData.Car("Mini", "Cooper", "4") with { Manufacturer = "", Scale = "" }];

        Assert.Equal(["4"], new CollectionQuery { NeedsDetailsOnly = true }.Apply(cars).Select(c => c.Id));
        Assert.True(new CollectionQuery { NeedsDetailsOnly = true }.HasFilters);
    }

    [Fact]
    public void Series_filter_limited_filter_and_search()
    {
        ModelCar[] cars =
        [
            TestData.Car("Ford", "GT40", "1") with { Series = "Corgi Classics", CatalogueNumber = "VA10601", EditionSize = 5000 },
            TestData.Car("Ford", "Capri", "2") with { Series = "Vanguards" },
            TestData.Car("Ford", "Escort", "3"),
        ];

        Assert.Equal(["1"], new CollectionQuery { Series = "corgi classics" }.Apply(cars).Select(c => c.Id));
        Assert.Equal(["1"], new CollectionQuery { LimitedOnly = true }.Apply(cars).Select(c => c.Id));
        Assert.Equal(["1"], new CollectionQuery { Search = "va10601" }.Apply(cars).Select(c => c.Id));
        Assert.Equal(["2"], new CollectionQuery { Search = "vanguards" }.Apply(cars).Select(c => c.Id));
    }
}
