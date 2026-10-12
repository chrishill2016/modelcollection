using System.Text;
using System.Text.Json;

namespace ModelCollection.Tests.Services;

public class BackupServiceTests
{
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-05-01T10:00:00Z"));

    private (LocalStore Store, BackupService Backup) Create()
    {
        var store = new LocalStore(new InMemoryIndexedDb(), _clock);
        return (store, new BackupService(store, new BrowserInterop(null!), _clock));
    }

    [Fact]
    public async Task Backup_round_trips_cars_and_photos()
    {
        var (source, sourceBackup) = Create();
        var car = await source.SaveCarAsync(TestData.Car() with { Tags = ["Le Mans"], PurchasePrice = 12.5m, PurchaseDate = new DateOnly(2001, 3, 4) });
        var photo = new CarPhoto("p1", car.Id, "data:full", _clock.GetUtcNow());
        await source.AddPhotoAsync(photo, photo with { DataUrl = "data:thumb" });
        var json = JsonSerializer.Serialize(await sourceBackup.CreateBackupAsync(), BackupService.JsonOptions);

        var (target, targetBackup) = Create();
        var count = await targetBackup.ImportJsonAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(1, count);
        var restored = await target.GetCarAsync(car.Id);
        Assert.Equal(car, restored);
        Assert.Equal("data:full", Assert.Single(await target.GetPhotosAsync(car.Id)).DataUrl);
        Assert.Equal("data:thumb", Assert.Single(await target.GetAllThumbnailsAsync()).DataUrl);
        Assert.Equal(1, await target.GetPendingCountAsync());
    }

    [Fact]
    public async Task Deleted_cars_are_left_out_of_backups()
    {
        var (store, backup) = Create();
        var car = await store.SaveCarAsync(TestData.Car());
        await store.DeleteCarAsync(car.Id);

        Assert.Empty((await backup.CreateBackupAsync()).Cars);
    }

    [Fact]
    public async Task Rejects_backups_from_newer_versions()
    {
        var (_, backup) = Create();
        var json = """{"formatVersion": 99, "exported": "2026-01-01T00:00:00Z", "cars": [], "photos": [], "thumbnails": []}""";

        await Assert.ThrowsAsync<InvalidDataException>(() => backup.ImportJsonAsync(new MemoryStream(Encoding.UTF8.GetBytes(json))));
    }
}
