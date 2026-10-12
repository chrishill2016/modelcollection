using Microsoft.Extensions.DependencyInjection;
using Bunit;
using ModelCollection.Features.Collection;

namespace ModelCollection.Tests.Components;

public class CollectionPageTests : ComponentTestBase
{
    [Fact]
    public void Empty_collection_invites_adding_a_car()
    {
        var cut = Render<CollectionPage>();

        cut.WaitForAssertion(() => Assert.Contains("No cars yet", cut.Markup));
    }

    [Fact]
    public async Task Shows_a_card_per_car_with_count()
    {
        await Store.SaveCarAsync(TestData.Car("Ferrari", "250 GTO"));
        await Store.SaveCarAsync(TestData.Car("Porsche", "911") with { HasBox = true });

        var cut = Render<CollectionPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("2 cars", cut.Find("[data-testid=car-count]").TextContent.Trim());
            Assert.Equal(2, cut.FindComponents<CarCard>().Count);
            Assert.Contains("1962 Porsche 911", cut.Markup);
        });
    }

    [Fact]
    public async Task Refreshes_when_a_car_is_added_elsewhere()
    {
        var cut = Render<CollectionPage>();
        cut.WaitForAssertion(() => Assert.Contains("No cars yet", cut.Markup));

        await cut.InvokeAsync(() => Store.SaveCarAsync(TestData.Car()));

        cut.WaitForAssertion(() => Assert.Single(cut.FindComponents<CarCard>()));
    }

    [Fact]
    public async Task Filters_are_remembered()
    {
        await Store.SaveCarAsync(TestData.Car("Ferrari"));
        await Store.SaveCarAsync(TestData.Car("Porsche"));
        Services.GetRequiredService<CollectionViewState>().Query = new CollectionQuery { Search = "porsche" };

        var cut = Render<CollectionPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1 of 2 cars", cut.Find("[data-testid=car-count]").TextContent.Trim());
            Assert.Single(cut.FindComponents<CarCard>());
        });
    }
}
