namespace ModelCollection.Services;

public interface ISyncService
{
    bool IsOnline { get; }
    bool IsSyncing { get; }
    int PendingCount { get; }
    DateTimeOffset? LastSuccessfulSync { get; }
    string? LastError { get; }

    /// <summary>Why syncing isn't possible right now, or null if it is.</summary>
    string? BlockedReason { get; }

    event Action? StateChanged;

    /// <summary>Messages worth showing the user (sync failed, back online and synced).</summary>
    event Action<SyncNotification>? Notification;

    /// <summary>Hooks up reconnect/change triggers and syncs once on start.</summary>
    Task StartAsync();

    Task<SyncResult> SyncNowAsync();
}

public sealed record SyncNotification(string Message, bool IsError);

public sealed record SyncResult(bool Success, int Pushed, int Pulled, string? Error = null, bool Skipped = false)
{
    public static SyncResult Skip(string reason) => new(false, 0, 0, reason, Skipped: true);
}
