using Microsoft.Extensions.Logging.Abstractions;

namespace ModelCollection.Tests.Services;

public class PhotoSyncTests : IDisposable
{
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-05-01T10:00:00Z"));
    private readonly LocalStore _local;
    private readonly FakeRemoteStore _remote = new();
    private readonly FakeConnectivity _connectivity = new();
    private readonly FakeCollections _collections = new();
    private readonly SyncService _sync;
    private readonly PhotoService _photos;

    public PhotoSyncTests()
    {
        _local = new LocalStore(new InMemoryIndexedDb(), _clock);
        _sync = new SyncService(_local, _remote, _connectivity, new FakeAuth(), _collections, _clock, NullLogger<SyncService>.Instance);
        _photos = new PhotoService(new BrowserInterop(null!), _local, _clock, _remote, _collections, _connectivity, NullLogger<PhotoService>.Instance);
    }

    public void Dispose() => _sync.Dispose();

    private async Task<CarPhoto> AddLocalPhotoAsync(string id, string carId = "car")
    {
        var photo = new CarPhoto(id, carId, $"data:full-{id}", _clock.GetUtcNow());
        await _local.AddPhotoAsync(photo, photo with { DataUrl = $"data:thumb-{id}" });
        return photo;
    }

    [Fact]
    public async Task New_photo_is_uploaded_with_its_thumbnail()
    {
        await AddLocalPhotoAsync("p1");
        Assert.Equal(1, await _local.GetPendingPhotoCountAsync());

        var result = await _sync.SyncNowAsync();

        Assert.Equal(1, result.PhotosPushed);
        Assert.Equal("data:full-p1", _remote.Files["photos/p1"]);
        Assert.Equal("data:thumb-p1", _remote.Files["thumbs/p1"]);
        Assert.False(_remote.Photos["p1"].Photo.IsDeleted);
        Assert.Equal(0, await _local.GetPendingPhotoCountAsync());
        Assert.Equal(0, _sync.PendingCount);
    }

    [Fact]
    public async Task Photos_taken_before_sync_existed_are_uploaded_once()
    {
        // Simulate an old photo with no outbox entry.
        await AddLocalPhotoAsync("old");
        await _local.CompletePhotoOutboxAsync((await _local.GetPhotoOutboxAsync())[0]);

        await _sync.SyncNowAsync();
        Assert.True(_remote.Photos.ContainsKey("old"));

        _remote.Photos.Clear();
        await _sync.SyncNowAsync();
        Assert.Empty(_remote.Photos);
    }

    [Fact]
    public async Task Photos_from_another_device_arrive_as_thumbnails()
    {
        _remote.UploadFromOtherDevice("p2", "car", _clock.GetUtcNow());

        var result = await _sync.SyncNowAsync();

        Assert.Equal(1, result.PhotosPulled);
        Assert.Equal("data:thumb-p2", (await _local.GetThumbnailAsync("p2"))!.DataUrl);
        Assert.Null(await _local.GetPhotoAsync("p2"));
        Assert.Equal(0, await _local.GetPendingPhotoCountAsync());
    }

    [Fact]
    public async Task Full_image_is_downloaded_on_demand_and_kept()
    {
        _remote.UploadFromOtherDevice("p2", "car", _clock.GetUtcNow());
        await _sync.SyncNowAsync();
        var thumbnail = (await _local.GetThumbnailAsync("p2"))!;
        var downloadsBefore = _remote.Downloads;

        Assert.Equal("data:full-p2", await _photos.GetFullImageAsync(thumbnail));
        Assert.Equal("data:full-p2", await _photos.GetFullImageAsync(thumbnail));

        Assert.Equal(downloadsBefore + 1, _remote.Downloads);
        Assert.Equal(0, await _local.GetPendingPhotoCountAsync());
    }

    [Fact]
    public async Task Full_image_is_unavailable_offline()
    {
        _remote.UploadFromOtherDevice("p2", "car", _clock.GetUtcNow());
        await _sync.SyncNowAsync();
        _connectivity.Set(false);

        Assert.Null(await _photos.GetFullImageAsync((await _local.GetThumbnailAsync("p2"))!));
    }

    [Fact]
    public async Task Deleting_a_photo_deletes_it_for_everyone()
    {
        await AddLocalPhotoAsync("p1");
        await _sync.SyncNowAsync();

        await _local.DeletePhotoAsync("p1");
        await _sync.SyncNowAsync();

        Assert.True(_remote.Photos["p1"].Photo.IsDeleted);
        Assert.False(_remote.Files.ContainsKey("photos/p1"));
        Assert.Equal(0, await _local.GetPendingPhotoCountAsync());
    }

    [Fact]
    public async Task Photo_deleted_on_another_device_is_removed_here()
    {
        await AddLocalPhotoAsync("p1");
        await _sync.SyncNowAsync();
        await _local.SaveDownloadedPhotoAsync(new CarPhoto("p1", "car", "data:full-p1", _clock.GetUtcNow()));

        _remote.DeleteFromOtherDevice("p1");
        await _sync.SyncNowAsync();

        Assert.Null(await _local.GetThumbnailAsync("p1"));
        Assert.Null(await _local.GetPhotoAsync("p1"));
    }

    [Fact]
    public async Task Deleting_a_car_deletes_its_photos_everywhere()
    {
        var car = await _local.SaveCarAsync(TestData.Car(id: "car"));
        await AddLocalPhotoAsync("p1");
        _remote.UploadFromOtherDevice("p2", "car", _clock.GetUtcNow());
        await _sync.SyncNowAsync();
        _clock.Advance(TimeSpan.FromMinutes(1));

        await _local.DeleteCarAsync(car.Id);
        await _sync.SyncNowAsync();

        Assert.True(_remote.Photos["p1"].Photo.IsDeleted);
        Assert.True(_remote.Photos["p2"].Photo.IsDeleted);
        Assert.Empty(await _local.GetAllThumbnailsAsync());
    }

    [Fact]
    public async Task Photo_deleted_here_before_sync_is_not_downloaded_again()
    {
        _remote.UploadFromOtherDevice("p2", "car", _clock.GetUtcNow());
        await _sync.SyncNowAsync();
        _connectivity.Set(false);
        await _local.DeletePhotoAsync("p2");
        _remote.Photos["p2"] = (_remote.Photos["p2"].Photo, 99_999); // touched remotely meanwhile

        _connectivity.Set(true);
        await _sync.SyncNowAsync();

        Assert.Null(await _local.GetThumbnailAsync("p2"));
        Assert.True(_remote.Photos["p2"].Photo.IsDeleted);
    }

    [Fact]
    public async Task Without_cloud_storage_photos_stay_local_and_are_not_counted()
    {
        _remote.SupportsPhotos = false;
        await _sync.StartAsync();

        await AddLocalPhotoAsync("p1");
        await _sync.SyncNowAsync();

        Assert.Empty(_remote.Photos);
        Assert.Equal(0, _sync.PendingCount);
        Assert.Equal(1, await _local.GetPendingPhotoCountAsync());
    }

    [Fact]
    public async Task Offline_photos_upload_on_reconnect()
    {
        await _sync.StartAsync();
        _connectivity.Set(false);
        await AddLocalPhotoAsync("p1");
        Assert.Equal(1, _sync.PendingCount);

        _connectivity.Set(true);

        Assert.True(_remote.Photos.ContainsKey("p1"));
        Assert.Equal(0, _sync.PendingCount);
    }

    [Fact]
    public async Task Failed_downloads_are_retried_and_do_not_block_uploads()
    {
        _remote.UploadFromOtherDevice("theirs", "car", _clock.GetUtcNow());
        await AddLocalPhotoAsync("mine");
        _remote.FailDownloads = true;

        var failed = await _sync.SyncNowAsync();

        Assert.False(failed.Success);
        Assert.Contains("Couldn't download some photos", failed.Error);
        Assert.True(_remote.Photos.ContainsKey("mine"));
        Assert.Null(await _local.GetThumbnailAsync("theirs"));

        _remote.FailDownloads = false;
        var retried = await _sync.SyncNowAsync();

        Assert.True(retried.Success);
        Assert.NotNull(await _local.GetThumbnailAsync("theirs"));
    }
}
