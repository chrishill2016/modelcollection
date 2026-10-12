using ModelCollection.Services;

namespace ModelCollection.Tests.Fakes;

public sealed class FakeSyncService : ISyncService
{
    public bool IsOnline { get; set; } = true;
    public bool IsSyncing { get; set; }
    public int PendingCount { get; set; }
    public DateTimeOffset? LastSuccessfulSync { get; set; }
    public string? LastError { get; set; }
    public string? BlockedReason { get; set; }
    public int SyncCalls { get; private set; }

    public event Action? StateChanged;
    public event Action<SyncNotification>? Notification;

    public Task StartAsync() => Task.CompletedTask;

    public Task<SyncResult> SyncNowAsync()
    {
        SyncCalls++;
        return Task.FromResult(new SyncResult(true, 0, 0));
    }

    public void RaiseStateChanged() => StateChanged?.Invoke();
    public void Notify(SyncNotification n) => Notification?.Invoke(n);
}
