using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using ModelCollection.Features.CarDetail;

namespace ModelCollection.Tests.Components;

public class CarDetailLoadingTests : ComponentTestBase
{
    /// <summary>A photo service whose full-size download only finishes when the test says so (a slow connection).</summary>
    private sealed class SlowPhotoService : IPhotoService
    {
        public TaskCompletionSource<string?> Download { get; private set; } = new();
        public int Requests { get; private set; }

        public Task<string?> GetFullImageAsync(CarPhoto thumbnail)
        {
            Requests++;
            Download = new TaskCompletionSource<string?>();
            return Download.Task;
        }

        public Task<CarPhoto> AddPhotoAsync(string carId, IBrowserFile file) => throw new NotSupportedException();
        public Task<PreparedPhoto> PrepareAsync(IBrowserFile file) => throw new NotSupportedException();
        public Task<CarPhoto> SavePreparedAsync(string carId, PreparedPhoto photo) => throw new NotSupportedException();
    }

    private readonly SlowPhotoService _photos = new();

    public CarDetailLoadingTests() => Services.AddSingleton<IPhotoService>(_photos);

    private async Task<ModelCar> CarWithPhotoAsync(params string[] ids)
    {
        var car = await Store.SaveCarAsync(TestData.Car("Aston Martin", "DB5", "car"));
        foreach (var id in ids)
        {
            Clock.Advance(TimeSpan.FromMinutes(1));
            var thumb = new CarPhoto(id, car.Id, $"data:thumb-{id}", Clock.GetUtcNow());
            await Store.SaveDownloadedThumbnailAsync(thumb); // synced from another device: no full image here yet
        }

        return car;
    }

    [Fact]
    public async Task Details_and_thumbnail_show_while_the_full_photo_is_still_downloading()
    {
        var car = await CarWithPhotoAsync("p1");

        var cut = Render<CarDetailPage>(p => p.Add(c => c.Id, car.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Aston Martin", cut.Markup);                                         // details are showing
            Assert.Equal("data:thumb-p1", cut.Find("[data-testid=main-photo]").GetAttribute("src")); // thumbnail meanwhile
            Assert.NotNull(cut.Find("[data-testid=full-photo-loading]"));                         // with a progress bar
        });

        await cut.InvokeAsync(() => _photos.Download.SetResult("data:full-p1"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("data:full-p1", cut.Find("[data-testid=main-photo]").GetAttribute("src"));
            Assert.Empty(cut.FindAll("[data-testid=full-photo-loading]"));
        });
    }

    [Fact]
    public async Task A_slow_download_for_a_photo_no_longer_selected_is_ignored()
    {
        var car = await CarWithPhotoAsync("p1", "p2");
        var cut = Render<CarDetailPage>(p => p.Add(c => c.Id, car.Id));
        cut.WaitForAssertion(() => Assert.Equal(1, _photos.Requests));
        var firstDownload = _photos.Download;

        await cut.InvokeAsync(() => cut.Find("[data-photo-id=p2]").Click());
        await cut.InvokeAsync(() => firstDownload.SetResult("data:full-p1"));

        cut.WaitForAssertion(() => Assert.Equal("data:thumb-p2", cut.Find("[data-testid=main-photo]").GetAttribute("src")));
    }

    [Fact]
    public async Task Offline_shows_the_thumbnail_with_a_note()
    {
        var car = await CarWithPhotoAsync("p1");
        var cut = Render<CarDetailPage>(p => p.Add(c => c.Id, car.Id));
        cut.WaitForAssertion(() => Assert.Equal(1, _photos.Requests));

        await cut.InvokeAsync(() => _photos.Download.SetResult(null));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Full-size photo will load when you're online", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid=full-photo-loading]"));
        });
    }
}
