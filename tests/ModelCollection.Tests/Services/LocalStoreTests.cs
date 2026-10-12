namespace ModelCollection.Tests.Services;

public class LocalStoreTests
{
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-05-01T10:00:00Z"));
    private readonly LocalStore _store;

    public LocalStoreTests() => _store = new LocalStore(new InMemoryIndexedDb(), _clock);

    [Fact]
    public async Task SaveCar_stamps_last_modified_and_queues_for_sync()
    {
        var saved = await _store.SaveCarAsync(TestData.Car());

        Assert.Equal(_clock.GetUtcNow(), saved.LastModified);
        Assert.Equal(1, await _store.GetPendingCountAsync());
        Assert.Equal(saved, await _store.GetCarAsync(saved.Id));
    }

    [Fact]
    public async Task Saving_the_same_car_twice_queues_it_once()
    {
        var car = await _store.SaveCarAsync(TestData.Car());
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _store.SaveCarAsync(car with { Colour = "Blue" });

        var outbox = await _store.GetOutboxAsync();
        Assert.Single(outbox);
        Assert.Equal(_clock.GetUtcNow(), outbox[0].Queued);
    }

    [Fact]
    public async Task SaveCar_moves_timestamp_forward_even_if_clock_is_behind()
    {
        var future = _clock.GetUtcNow().AddHours(1);
        await _store.ApplyRemoteAsync(TestData.Car(id: "a", modified: future));

        var saved = await _store.SaveCarAsync((await _store.GetCarAsync("a"))! with { Colour = "Green" });

        Assert.True(saved.LastModified > future);
    }

    [Fact]
    public async Task DeleteCar_soft_deletes_queues_and_removes_photos()
    {
        var car = await _store.SaveCarAsync(TestData.Car());
        var photo = new CarPhoto("p1", car.Id, "data:full", _clock.GetUtcNow());
        await _store.AddPhotoAsync(photo, photo with { DataUrl = "data:thumb" });
        await _store.CompleteOutboxAsync((await _store.GetOutboxAsync())[0]);
        _clock.Advance(TimeSpan.FromSeconds(1));

        await _store.DeleteCarAsync(car.Id);

        Assert.Empty(await _store.GetCarsAsync());
        Assert.True((await _store.GetCarAsync(car.Id))!.IsDeleted);
        Assert.Equal(1, await _store.GetPendingCountAsync());
        Assert.Empty(await _store.GetPhotosAsync(car.Id));
        Assert.Empty(await _store.GetAllThumbnailsAsync());
    }

    [Fact]
    public async Task ApplyRemote_newer_copy_wins_and_drops_pending_local_edit()
    {
        var local = await _store.SaveCarAsync(TestData.Car(id: "a"));
        var remote = local with { Colour = "Remote blue", LastModified = local.LastModified.AddMinutes(5) };

        Assert.True(await _store.ApplyRemoteAsync(remote));

        Assert.Equal("Remote blue", (await _store.GetCarAsync("a"))!.Colour);
        Assert.Equal(0, await _store.GetPendingCountAsync());
    }

    [Fact]
    public async Task ApplyRemote_older_or_equal_copy_is_ignored()
    {
        var local = await _store.SaveCarAsync(TestData.Car(id: "a") with { Colour = "Local" });

        Assert.False(await _store.ApplyRemoteAsync(local with { Colour = "Old", LastModified = local.LastModified.AddMinutes(-5) }));
        Assert.False(await _store.ApplyRemoteAsync(local with { Colour = "Same time" }));

        Assert.Equal("Local", (await _store.GetCarAsync("a"))!.Colour);
        Assert.Equal(1, await _store.GetPendingCountAsync());
    }

    [Fact]
    public async Task ApplyRemote_deletion_removes_local_photos()
    {
        var car = await _store.SaveCarAsync(TestData.Car(id: "a"));
        var photo = new CarPhoto("p1", "a", "data:full", _clock.GetUtcNow());
        await _store.AddPhotoAsync(photo, photo);

        await _store.ApplyRemoteAsync(car with { IsDeleted = true, LastModified = car.LastModified.AddMinutes(1) });

        Assert.Empty(await _store.GetCarsAsync());
        Assert.Empty(await _store.GetAllPhotosAsync());
    }

    [Fact]
    public async Task CompleteOutbox_keeps_entry_if_car_was_edited_again()
    {
        var car = await _store.SaveCarAsync(TestData.Car());
        var entry = (await _store.GetOutboxAsync())[0];
        _clock.Advance(TimeSpan.FromSeconds(5));
        await _store.SaveCarAsync(car with { Colour = "Edited while syncing" });

        await _store.CompleteOutboxAsync(entry);

        Assert.Equal(1, await _store.GetPendingCountAsync());
    }

    [Fact]
    public async Task CompleteOutbox_removes_unchanged_entry()
    {
        await _store.SaveCarAsync(TestData.Car());

        await _store.CompleteOutboxAsync((await _store.GetOutboxAsync())[0]);

        Assert.Equal(0, await _store.GetPendingCountAsync());
    }

    [Fact]
    public async Task ImportCar_applies_newer_copies_and_queues_them()
    {
        var older = TestData.Car(id: "a", modified: DateTimeOffset.Parse("2026-01-01Z"));
        await _store.ApplyRemoteAsync(older);

        Assert.False(await _store.ImportCarAsync(older with { Colour = "Older", LastModified = older.LastModified.AddDays(-1) }));
        Assert.True(await _store.ImportCarAsync(older with { Colour = "Newer", LastModified = older.LastModified.AddDays(1) }));

        Assert.Equal("Newer", (await _store.GetCarAsync("a"))!.Colour);
        Assert.Equal(1, await _store.GetPendingCountAsync());
    }

    [Fact]
    public async Task Changed_is_raised_on_save()
    {
        var raised = 0;
        _store.Changed += () => raised++;

        await _store.SaveCarAsync(TestData.Car());

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task Meta_values_can_be_set_and_cleared()
    {
        await _store.SetMetaAsync("k", "v");
        Assert.Equal("v", await _store.GetMetaAsync("k"));

        await _store.SetMetaAsync("k", null);
        Assert.Null(await _store.GetMetaAsync("k"));
    }

    [Fact]
    public async Task Photos_are_returned_in_order_taken()
    {
        var t = _clock.GetUtcNow();
        await _store.AddPhotoAsync(new CarPhoto("p2", "a", "2", t.AddMinutes(1)), new CarPhoto("p2", "a", "t2", t.AddMinutes(1)));
        await _store.AddPhotoAsync(new CarPhoto("p1", "a", "1", t), new CarPhoto("p1", "a", "t1", t));
        await _store.AddPhotoAsync(new CarPhoto("p3", "b", "3", t), new CarPhoto("p3", "b", "t3", t));

        Assert.Equal(["p1", "p2"], (await _store.GetPhotosAsync("a")).Select(p => p.Id));
    }
}
