using Microsoft.Extensions.DependencyInjection;
using Bunit;
using ModelCollection.Features.CarDetail;

namespace ModelCollection.Tests.Components;

public class NavigationTests : ComponentTestBase
{
    [Fact]
    public async Task Car_page_back_arrow_links_to_the_collection()
    {
        var car = await Store.SaveCarAsync(TestData.Car());

        var cut = Render<CarDetailPage>(p => p.Add(c => c.Id, car.Id));

        cut.WaitForAssertion(() =>
            // An empty href would point at the current page; "./" is the app root under <base href>.
            Assert.Equal("./", cut.Find("a[aria-label='Back to collection']").GetAttribute("href")));
    }

    [Fact]
    public async Task Cancelling_an_edit_opened_directly_goes_to_the_car_page()
    {
        var car = await Store.SaveCarAsync(TestData.Car());
        var cut = Render<CarEditPage>(p => p.Add(c => c.Id, car.Id));

        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click());

        Assert.EndsWith($"cars/{car.Id}", Navigation.Uri);
    }

    [Fact]
    public async Task Cancelling_an_edit_reached_from_the_car_page_goes_back_in_history()
    {
        var car = await Store.SaveCarAsync(TestData.Car());
        Services.GetRequiredService<AppNavigation>(); // the layout creates it at startup
        Navigation.NavigateTo($"cars/{car.Id}");
        Navigation.NavigateTo($"cars/{car.Id}/edit");
        var cut = Render<CarEditPage>(p => p.Add(c => c.Id, car.Id));
        var goBack = JSInterop.SetupModule("./js/browser.js").SetupVoid("goBack");
        goBack.SetVoidResult();

        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click());

        goBack.VerifyInvoke("goBack");
    }
}
