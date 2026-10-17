using ModelCollection.Features.CarDetail;

namespace ModelCollection.Tests.Features;

public class CarFormModelTests
{
    [Fact]
    public void Round_trips_a_car_and_keeps_sync_metadata()
    {
        var car = TestData.Car() with
        {
            Tags = ["a", "b"], PurchaseDate = new DateOnly(2020, 2, 29), HasBox = true,
            Series = "Corgi Classics", CatalogueNumber = "VA10601", EditionNumber = 12, EditionSize = 500, PhotoOrder = ["p2", "p1"],
        };

        var result = CarFormModel.From(car).ToCar(car);

        Assert.Equal(car, result);
    }

    [Fact]
    public void Trims_text_and_splits_tags()
    {
        var form = new CarFormModel { Make = "  Ferrari ", Tags = "Le Mans, , gift,le mans " };

        var car = form.ToCar();

        Assert.Equal("Ferrari", car.Make);
        Assert.Equal(["Le Mans", "gift"], car.Tags);
        Assert.Equal(form.Id, car.Id);
    }

    [Theory]
    [InlineData(5, 10, null)]
    [InlineData(10, 10, null)]
    [InlineData(11, 10, "Can't be more than the edition size (10)")]
    [InlineData(11, null, null)]
    public void Edition_number_cannot_exceed_edition_size(int? number, int? size, string? expected) =>
        Assert.Equal(expected, new CarFormModel { EditionNumber = number, EditionSize = size }.EditionError);
}
