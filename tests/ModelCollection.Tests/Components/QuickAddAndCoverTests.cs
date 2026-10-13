using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using ModelCollection.Features.CarDetail;
using ModelCollection.Features.Collection;

namespace ModelCollection.Tests.Components;

public class QuickAddAndCoverTests : ComponentTestBase
{
    private async Task<(ModelCar Car, CarPhoto[] Photos)> CarWithPhotosAsync()
    {
        var car = await Store.SaveCarAsync(TestData.Car(id: "car"));
        var photos = new List<CarPhoto>();
        foreach (var id in new[] { "p1", "p2", "p3" })
        {
            Clock.Advance(TimeSpan.FromMinutes(1));
            var photo = new CarPhoto(id, car.Id, $"data:full-{id}", Clock.GetUtcNow());
            await Store.AddPhotoAsync(photo, photo with { DataUrl = $"data:thumb-{id}" });
            photos.Add(photo);
        }

        return (car, photos.ToArray());
    }

    private static string[] GalleryOrder(IRenderedComponent<CarDetailPage> cut) =>
        cut.FindAll(".photo-gallery img").Select(i => i.GetAttribute("data-photo-id")!).ToArray();

    [Fact]
    public async Task Make_cover_moves_the_photo_first_and_saves_it_on_the_car()
    {
        var (car, _) = await CarWithPhotosAsync();
        var cut = Render<CarDetailPage>(p => p.Add(c => c.Id, car.Id));
        cut.WaitForAssertion(() => Assert.Equal(["p1", "p2", "p3"], GalleryOrder(cut)));

        await cut.InvokeAsync(() => cut.Find("[data-photo-id=p3]").Click());
        cut.WaitForAssertion(() => cut.FindAll("button").Single(b => b.TextContent.Contains("Make cover")));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Contains("Make cover")).Click());

        cut.WaitForAssertion(() => Assert.Equal(["p3", "p1", "p2"], GalleryOrder(cut)));
        Assert.Equal(["p3", "p1", "p2"], (await Store.GetCarAsync(car.Id))!.PhotoOrder);
        Assert.Equal(1, await Store.GetPendingCountAsync()); // the new order syncs like any edit
    }

    [Fact]
    public async Task Move_left_and_right_reorder_photos()
    {
        var (car, _) = await CarWithPhotosAsync();
        var cut = Render<CarDetailPage>(p => p.Add(c => c.Id, car.Id));
        cut.WaitForAssertion(() => Assert.Equal(3, GalleryOrder(cut).Length));

        await cut.InvokeAsync(() => cut.Find("[data-photo-id=p1]").Click());
        await cut.InvokeAsync(() => cut.Find("[aria-label='Move photo right']").Click());

        cut.WaitForAssertion(() => Assert.Equal(["p2", "p1", "p3"], GalleryOrder(cut)));
    }

    [Fact]
    public async Task Collection_card_shows_the_chosen_cover()
    {
        var (car, _) = await CarWithPhotosAsync();
        await Store.SaveCarAsync((await Store.GetCarAsync(car.Id))! with { PhotoOrder = ["p2"] });

        var cut = Render<CollectionPage>();

        cut.WaitForAssertion(() => Assert.Equal("data:thumb-p2", cut.Find(".car-card img").GetAttribute("src")));
    }

    [Fact]
    public async Task Quick_added_cars_are_marked_and_can_be_filtered()
    {
        await Store.SaveCarAsync(TestData.Car("Ferrari"));
        await Store.SaveCarAsync(new ModelCar { Make = "Mini" });

        var cut = Render<CollectionPage>();
        cut.WaitForAssertion(() => Assert.Contains("Needs details (1)", cut.Find("[data-testid=needs-details-filter]").TextContent));

        await cut.InvokeAsync(() => cut.Find("[data-testid=needs-details-filter]").Click());

        cut.WaitForAssertion(() =>
        {
            var card = Assert.Single(cut.FindComponents<CarCard>());
            Assert.Equal("Mini", card.Instance.Car.Make);
            Assert.Contains("Needs details", card.Markup);
        });
    }

    [Fact]
    public void Quick_add_starts_with_the_camera_and_shows_the_form_after_a_photo()
    {
        var cut = Render<QuickAddPage>();
        Assert.NotNull(cut.Find("[data-testid=camera-input]"));
        Assert.Empty(cut.FindAll("input[type=text]"));

        cut.FindComponents<InputFile>()[0].UploadFiles(InputFileContent.CreateFromBinary([1], "car.jpg", contentType: "image/jpeg"));

        cut.WaitForAssertion(() => Assert.Equal("data:thumb-car.jpg", cut.Find(".quick-add-photos img").GetAttribute("src")));
        Assert.Contains(cut.FindAll("button"), b => b.TextContent.Contains("Save & next"));
    }
}
