using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>
/// Pulls cloud changes then pushes the local outbox. Conflicts are last-write-wins on
/// <see cref="ModelCar.LastModified"/>: a newer cloud copy replaces the local one (and drops its
/// pending edit); otherwise the local edit is pushed.
/// </summary>
public sealed class SyncService(
    ILocalStore local,
    IRemoteStore remote,
    IConnectivityService connectivity,
    IAuthService auth,
    ICollectionService collections,
    TimeProvider clock,
    ILogger<SyncService> logger) : ISyncService, IDisposable
{
    internal const string LastSyncMetaKey = "lastSync";
    internal static string CursorMetaKey(string collectionId) => $"pullCursor:{collectionId}";
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _syncAgain;
    private bool _wentOffline;
    private bool _lastAttemptFailed;
    private ITimer? _timer;

    public bool IsOnline => connectivity.IsOnline;
    public bool IsSyncing { get; private set; }
    public int PendingCount { get; private set; }
    public DateTimeOffset? LastSuccessfulSync { get; private set; }
    public string? LastError { get; private set; }

    public string? BlockedReason =>
        !auth.IsConfigured ? "Cloud sync isn't set up"
        : !connectivity.IsOnline ? "Offline"
        : auth.User is null ? "Sign in to sync"
        : collections.Current is null ? "No shared collection yet"
        : null;

    public event Action? StateChanged;
    public event Action<SyncNotification>? Notification;

    public async Task StartAsync()
    {
        if (await local.GetMetaAsync(LastSyncMetaKey) is { } last && DateTimeOffset.TryParse(last, out var parsed))
        {
            LastSuccessfulSync = parsed;
        }

        await RefreshPendingAsync();

        connectivity.Changed += OnConnectivityChanged;
        local.Changed += OnLocalChanged;
        collections.Changed += OnCollectionChanged;
        auth.Changed += OnStateInputChanged;

        _timer = clock.CreateTimer(_ => _ = SyncNowAsync(), null, PollInterval, PollInterval);

        await SyncNowAsync();
    }

    public async Task<SyncResult> SyncNowAsync()
    {
        if (BlockedReason is { } reason)
        {
            StateChanged?.Invoke();
            return SyncResult.Skip(reason);
        }

        if (!await _gate.WaitAsync(0))
        {
            // A sync is running; make sure it goes round again to pick up the latest changes.
            _syncAgain = true;
            return SyncResult.Skip("Already syncing");
        }

        try
        {
            SyncResult result;
            var rounds = 0;
            do
            {
                _syncAgain = false;
                result = await RunOnceAsync();
            }
            // Go round again if edits arrived while syncing (capped so a stuck outbox can't loop forever).
            while (result.Success && (_syncAgain || PendingCount > 0) && ++rounds < 3);

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SyncResult> RunOnceAsync()
    {
        var collectionId = collections.Current!.Id;
        IsSyncing = true;
        StateChanged?.Invoke();
        try
        {
            var cursorKey = CursorMetaKey(collectionId);
            var cursor = long.TryParse(await local.GetMetaAsync(cursorKey), out var c) ? c : 0;

            var pulled = await remote.PullAsync(collectionId, cursor);
            var applied = 0;
            foreach (var car in pulled.Cars)
            {
                if (await local.ApplyRemoteAsync(car))
                {
                    applied++;
                }
            }

            var outbox = await local.GetOutboxAsync();
            var toPush = new List<ModelCar>();
            foreach (var entry in outbox)
            {
                if (await local.GetCarAsync(entry.CarId) is { } car)
                {
                    toPush.Add(car);
                }
            }

            if (toPush.Count > 0)
            {
                await remote.PushAsync(collectionId, toPush);
            }

            foreach (var entry in outbox)
            {
                await local.CompleteOutboxAsync(entry);
            }

            await local.SetMetaAsync(cursorKey, Math.Max(cursor, pulled.Cursor).ToString());
            LastSuccessfulSync = clock.GetUtcNow();
            await local.SetMetaAsync(LastSyncMetaKey, LastSuccessfulSync.Value.ToString("O"));
            LastError = null;

            if (_wentOffline)
            {
                _wentOffline = false;
                Notification?.Invoke(new SyncNotification(
                    toPush.Count > 0 ? $"Back online – {Plural(toPush.Count, "change")} synced" : "Back online – all synced",
                    IsError: false));
            }

            _lastAttemptFailed = false;
            return new SyncResult(true, toPush.Count, applied);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sync failed");
            LastError = ex.Message;
            // Only shout about the first failure in a run of them, so background retries don't spam.
            if (!_lastAttemptFailed)
            {
                Notification?.Invoke(new SyncNotification($"Sync failed: {ex.Message}", IsError: true));
            }

            _lastAttemptFailed = true;
            return new SyncResult(false, 0, 0, ex.Message);
        }
        finally
        {
            IsSyncing = false;
            await RefreshPendingAsync();
        }
    }

    private void OnConnectivityChanged(bool online)
    {
        if (!online)
        {
            _wentOffline = true;
            StateChanged?.Invoke();
            return;
        }

        _ = SyncNowAsync();
    }

    private void OnLocalChanged() => _ = OnLocalChangedAsync();

    private async Task OnLocalChangedAsync()
    {
        await RefreshPendingAsync();
        if (PendingCount > 0 && !IsSyncing)
        {
            await SyncNowAsync();
        }
    }

    private void OnCollectionChanged() => _ = SyncNowAsync();

    private void OnStateInputChanged() => StateChanged?.Invoke();

    private async Task RefreshPendingAsync()
    {
        PendingCount = await local.GetPendingCountAsync();
        StateChanged?.Invoke();
    }

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    public void Dispose()
    {
        _timer?.Dispose();
        connectivity.Changed -= OnConnectivityChanged;
        local.Changed -= OnLocalChanged;
        collections.Changed -= OnCollectionChanged;
        auth.Changed -= OnStateInputChanged;
    }
}
