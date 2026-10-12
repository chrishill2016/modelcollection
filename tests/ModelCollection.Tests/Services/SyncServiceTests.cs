using Microsoft.Extensions.Logging.Abstractions;

namespace ModelCollection.Tests.Services;

public class SyncServiceTests : IDisposable
{
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-05-01T10:00:00Z"));
    private readonly LocalStore _local;
    private readonly FakeRemoteStore _remote = new();
    private readonly FakeConnectivity _connectivity = new();
    private readonly FakeAuth _auth = new();
    private readonly FakeCollections _collections = new();
    private readonly SyncService _sync;
    private readonly List<SyncNotification> _notifications = [];

    public SyncServiceTests()
    {
        _local = new LocalStore(new InMemoryIndexedDb(), _clock);
        _sync = new SyncService(_local, _remote, _connectivity, _auth, _collections, _clock, NullLogger<SyncService>.Instance);
        _sync.Notification += _notifications.Add;
    }

    public void Dispose() => _sync.Dispose();

    [Fact]
    public async Task Pushes_queued_changes_and_clears_the_outbox()
    {
        var car = await _local.SaveCarAsync(TestData.Car());

        var result = await _sync.SyncNowAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.Pushed);
        Assert.Equal(car, _remote.Cars[car.Id].Car);
        Assert.Equal(0, await _local.GetPendingCountAsync());
        Assert.Equal(0, _sync.PendingCount);
        Assert.Equal(_clock.GetUtcNow(), _sync.LastSuccessfulSync);
    }

    [Fact]
    public async Task Pulls_cars_added_on_another_device()
    {
        _remote.WriteFromOtherDevice(TestData.Car("Porsche", "911", id: "remote"));

        var result = await _sync.SyncNowAsync();

        Assert.Equal(1, result.Pulled);
        Assert.Equal("Porsche", (await _local.GetCarAsync("remote"))!.Make);
    }

    [Fact]
    public async Task Newer_cloud_edit_beats_older_local_edit()
    {
        var local = await _local.SaveCarAsync(TestData.Car(id: "a") with { Colour = "Local red" });
        _remote.WriteFromOtherDevice(local with { Colour = "Cloud blue", LastModified = local.LastModified.AddMinutes(1) });

        var result = await _sync.SyncNowAsync();

        Assert.Equal(0, result.Pushed);
        Assert.Equal("Cloud blue", (await _local.GetCarAsync("a"))!.Colour);
        Assert.Equal("Cloud blue", _remote.Cars["a"].Car.Colour);
        Assert.Equal(0, _sync.PendingCount);
    }

    [Fact]
    public async Task Newer_local_edit_beats_older_cloud_edit()
    {
        var original = TestData.Car(id: "a", modified: _clock.GetUtcNow().AddHours(-2));
        _remote.WriteFromOtherDevice(original with { Colour = "Cloud blue", LastModified = original.LastModified.AddMinutes(1) });
        await _local.SaveCarAsync(original with { Colour = "Local red" });

        var result = await _sync.SyncNowAsync();

        Assert.Equal(1, result.Pushed);
        Assert.Equal("Local red", _remote.Cars["a"].Car.Colour);
        Assert.Equal("Local red", (await _local.GetCarAsync("a"))!.Colour);
    }

    [Fact]
    public async Task Deletions_sync_both_ways()
    {
        var mine = await _local.SaveCarAsync(TestData.Car(id: "mine"));
        _remote.WriteFromOtherDevice(TestData.Car(id: "theirs"));
        await _sync.SyncNowAsync();

        _clock.Advance(TimeSpan.FromMinutes(1));
        await _local.DeleteCarAsync("mine");
        var theirs = _remote.Cars["theirs"].Car;
        _remote.WriteFromOtherDevice(theirs with { IsDeleted = true, LastModified = _clock.GetUtcNow() });
        await _sync.SyncNowAsync();

        Assert.True(_remote.Cars["mine"].Car.IsDeleted);
        Assert.Empty(await _local.GetCarsAsync());
    }

    [Fact]
    public async Task Only_changes_since_last_pull_are_applied()
    {
        _remote.WriteFromOtherDevice(TestData.Car(id: "a"));
        await _sync.SyncNowAsync();

        var second = await _sync.SyncNowAsync();

        Assert.Equal(0, second.Pulled);
        Assert.NotNull(await _local.GetMetaAsync(SyncService.CursorMetaKey("col-1")));
    }

    [Fact]
    public async Task Offline_changes_wait_in_the_outbox()
    {
        _connectivity.Set(false);
        await _local.SaveCarAsync(TestData.Car());

        var result = await _sync.SyncNowAsync();

        Assert.True(result.Skipped);
        Assert.Equal("Offline", _sync.BlockedReason);
        Assert.Empty(_remote.Pushes);
        Assert.Equal(1, await _local.GetPendingCountAsync());
    }

    [Fact]
    public async Task Reconnecting_syncs_automatically_and_notifies()
    {
        await _sync.StartAsync();
        _connectivity.Set(false);
        await _local.SaveCarAsync(TestData.Car());
        await _local.SaveCarAsync(TestData.Car("Jaguar", "E-Type"));
        Assert.Equal(2, _sync.PendingCount);

        _connectivity.Set(true);

        Assert.Equal(2, _remote.Cars.Count);
        Assert.Equal(0, _sync.PendingCount);
        var notice = Assert.Single(_notifications);
        Assert.False(notice.IsError);
        Assert.Equal("Back online – 2 changes synced", notice.Message);
    }

    [Fact]
    public async Task Local_save_while_online_syncs_straight_away()
    {
        await _sync.StartAsync();

        await _local.SaveCarAsync(TestData.Car());

        Assert.Single(_remote.Cars);
        Assert.Equal(0, _sync.PendingCount);
    }

    [Fact]
    public async Task Start_syncs_once()
    {
        _remote.WriteFromOtherDevice(TestData.Car(id: "a"));

        await _sync.StartAsync();

        Assert.NotNull(await _local.GetCarAsync("a"));
    }

    [Fact]
    public async Task Polls_for_changes_from_other_devices()
    {
        await _sync.StartAsync();
        _remote.WriteFromOtherDevice(TestData.Car(id: "later"));

        _clock.Advance(SyncService.PollInterval);

        Assert.NotNull(await _local.GetCarAsync("later"));
    }

    [Fact]
    public async Task Failure_keeps_changes_queued_and_notifies_once()
    {
        await _local.SaveCarAsync(TestData.Car());
        _remote.FailWith = new InvalidOperationException("permission-denied");

        var first = await _sync.SyncNowAsync();
        await _sync.SyncNowAsync();

        Assert.False(first.Success);
        Assert.Equal("permission-denied", _sync.LastError);
        Assert.Equal(1, _sync.PendingCount);
        var notice = Assert.Single(_notifications);
        Assert.True(notice.IsError);

        _remote.FailWith = null;
        var recovered = await _sync.SyncNowAsync();
        Assert.True(recovered.Success);
        Assert.Null(_sync.LastError);
        Assert.Equal(0, _sync.PendingCount);
    }

    [Fact]
    public async Task Edit_made_during_a_push_is_synced_in_a_second_round()
    {
        var car = await _local.SaveCarAsync(TestData.Car(id: "a"));
        _remote.DuringPush = async () =>
        {
            _clock.Advance(TimeSpan.FromSeconds(1));
            await _local.SaveCarAsync(car with { Colour = "Changed mid-sync" });
        };

        await _sync.SyncNowAsync();

        Assert.Equal("Changed mid-sync", _remote.Cars["a"].Car.Colour);
        Assert.Equal(0, _sync.PendingCount);
    }

    [Fact]
    public async Task Does_not_sync_when_signed_out()
    {
        _auth.User = null;
        await _local.SaveCarAsync(TestData.Car());

        var result = await _sync.SyncNowAsync();

        Assert.True(result.Skipped);
        Assert.Equal("Sign in to sync", _sync.BlockedReason);
        Assert.Empty(_remote.Pushes);
    }

    [Fact]
    public async Task Does_not_sync_without_a_collection()
    {
        _collections.Current = null;

        var result = await _sync.SyncNowAsync();

        Assert.True(result.Skipped);
        Assert.Equal("No shared collection yet", _sync.BlockedReason);
    }

    [Fact]
    public async Task Does_not_sync_when_cloud_is_not_configured()
    {
        _auth.IsConfigured = false;

        Assert.True((await _sync.SyncNowAsync()).Skipped);
        Assert.Equal("Cloud sync isn't set up", _sync.BlockedReason);
    }

    [Fact]
    public async Task Changes_made_before_joining_a_collection_are_uploaded_on_join()
    {
        _collections.Current = null;
        await _sync.StartAsync();
        await _local.SaveCarAsync(TestData.Car(id: "early"));

        _collections.Current = new SharedCollection("col-2", "New", ["dad@example.com"]);
        _collections.RaiseChanged();

        Assert.True(_remote.Cars.ContainsKey("early"));
    }

    [Fact]
    public async Task Last_sync_time_survives_restart()
    {
        await _sync.SyncNowAsync();
        using var restarted = new SyncService(_local, _remote, _connectivity, _auth, _collections, _clock, NullLogger<SyncService>.Instance);

        _connectivity.Set(false);
        await restarted.StartAsync();

        Assert.Equal(_clock.GetUtcNow(), restarted.LastSuccessfulSync);
    }
}
