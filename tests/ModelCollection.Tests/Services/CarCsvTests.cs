namespace ModelCollection.Tests.Services;

public class CarCsvTests
{
    [Fact]
    public void Writes_header_and_one_row_per_car()
    {
        var csv = CarCsv.Write([TestData.Car() with { HasBox = true, PurchasePrice = 9.99m, Tags = ["a", "b"] }]);
        var lines = csv.TrimEnd().Split(Environment.NewLine);

        Assert.Equal(2, lines.Length);
        Assert.StartsWith("Make,Model,Year", lines[0]);
        Assert.Equal("Ferrari,250 GTO,1962,Corgi,1:43,Red,Excellent,Yes,,9.99,,,a; b,", lines[1]);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("has, comma", "\"has, comma\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    [InlineData("-5", "-5")]
    [InlineData(null, "")]
    public void Escapes_values(string? input, string expected) => Assert.Equal(expected, CarCsv.Escape(input));
}
