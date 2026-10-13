namespace ModelCollection.Tests.Features;

public class PhotoOrderingTests
{
    private static readonly DateTimeOffset T = DateTimeOffset.Parse("2026-01-01Z");

    private static readonly CarPhoto[] Photos =
    [
        new("c", "car", "", T.AddMinutes(3)),
        new("a", "car", "", T.AddMinutes(1)),
        new("b", "car", "", T.AddMinutes(2)),
    ];

    private static string[] Ids(IEnumerable<CarPhoto> photos) => photos.Select(p => p.Id).ToArray();

    [Fact]
    public void Without_an_order_photos_are_oldest_first() =>
        Assert.Equal(["a", "b", "c"], Ids(PhotoOrdering.Sort(Photos, [])));

    [Fact]
    public void Chosen_order_comes_first_then_the_rest_oldest_first() =>
        Assert.Equal(["c", "a", "b"], Ids(PhotoOrdering.Sort(Photos, ["c"])));

    [Fact]
    public void Ids_of_deleted_photos_are_ignored() =>
        Assert.Equal(["b", "a", "c"], Ids(PhotoOrdering.Sort(Photos, ["gone", "b"])));

    [Fact]
    public void Make_cover_moves_the_photo_to_the_front()
    {
        var sorted = PhotoOrdering.Sort(Photos, []);
        Assert.Equal(["c", "a", "b"], PhotoOrdering.MakeCover(sorted, "c"));
    }

    [Theory]
    [InlineData("b", -1, new[] { "b", "a", "c" })]
    [InlineData("b", 1, new[] { "a", "c", "b" })]
    [InlineData("a", -1, new[] { "a", "b", "c" })]
    [InlineData("c", 1, new[] { "a", "b", "c" })]
    public void Move_swaps_with_the_neighbour_and_stops_at_the_ends(string id, int direction, string[] expected) =>
        Assert.Equal(expected, PhotoOrdering.Move(PhotoOrdering.Sort(Photos, []), id, direction));
}
