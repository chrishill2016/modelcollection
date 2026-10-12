using Bunit;
using ModelCollection.Features.Collection;

namespace ModelCollection.Tests.Components;

public class CarCardTests : ComponentTestBase
{
    [Fact]
    public void Shows_name_details_and_links_to_the_car()
    {
        var car = TestData.Car() with { HasBox = true, Condition = CarCondition.NearMint };

        var cut = Render<CarCard>(p => p.Add(c => c.Car, car));

        Assert.Contains("1962 Ferrari 250 GTO", cut.Markup);
        Assert.Contains("Corgi · 1:43 · Red", cut.Markup);
        Assert.Contains("Near mint", cut.Markup);
        Assert.Contains("Boxed", cut.Markup);
        Assert.Equal($"cars/{car.Id}", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Shows_thumbnail_when_there_is_one()
    {
        var cut = Render<CarCard>(p => p
            .Add(c => c.Car, TestData.Car())
            .Add(c => c.Thumbnail, "data:image/jpeg;base64,xyz"));

        Assert.Equal("data:image/jpeg;base64,xyz", cut.Find("img").GetAttribute("src"));
    }
}
