using Bunit;
using ModelCollection.Features.Stats;

namespace ModelCollection.Tests.Components;

public class StatsPageTests : ComponentTestBase
{
    [Fact]
    public async Task Shows_totals_and_breakdowns()
    {
        await Store.SaveCarAsync(TestData.Car() with { PurchasePrice = 12, EstimatedValue = 40, HasBox = true });
        await Store.SaveCarAsync(TestData.Car("Ford", "GT40") with { Manufacturer = "Minichamps", PurchasePrice = 30 });

        var cut = Render<StatsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("£42", cut.Markup);
            Assert.Contains("£40", cut.Markup);
            Assert.Contains("50% of the collection", cut.Markup);
            Assert.Contains("Minichamps", cut.Markup);
            Assert.Equal(4, cut.FindComponents<BreakdownList>().Count);
        });
    }
}
