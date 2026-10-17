namespace ModelCollection.Tests.Features;

public class EditionTests
{
    [Theory]
    [InlineData(1234, 5000, "No. 1,234 of 5,000")]
    [InlineData(null, 5000, "Limited edition of 5,000")]
    [InlineData(7, null, "No. 7")]
    [InlineData(null, null, null)]
    public void Edition_label(int? number, int? size, string? expected) =>
        Assert.Equal(expected, (TestData.Car() with { EditionNumber = number, EditionSize = size }).EditionLabel);

    [Fact]
    public void Limited_edition_needs_an_edition_size()
    {
        Assert.True((TestData.Car() with { EditionSize = 500 }).IsLimitedEdition);
        Assert.False((TestData.Car() with { EditionNumber = 7 }).IsLimitedEdition);
    }
}
