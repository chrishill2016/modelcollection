using Bunit;
using ModelCollection.Shared;

namespace ModelCollection.Tests.Components;

public class SyncStatusIndicatorTests : ComponentTestBase
{
    [Fact]
    public void Shows_online_with_no_pending_changes()
    {
        var cut = Render<SyncStatusIndicator>();

        var button = cut.Find("[data-testid=sync-indicator]");
        Assert.Contains("Online", button.TextContent);
        Assert.Contains("0 waiting", button.GetAttribute("aria-label"));
        Assert.Contains("last synced never", button.GetAttribute("aria-label"));
    }

    [Fact]
    public void Shows_offline_with_pending_count_and_last_sync_time()
    {
        Sync.IsOnline = false;
        Sync.PendingCount = 3;
        Sync.LastSuccessfulSync = DateTimeOffset.Parse("2026-05-01T09:30:00Z");

        var cut = Render<SyncStatusIndicator>();

        var button = cut.Find("[data-testid=sync-indicator]");
        Assert.Contains("Offline", button.TextContent);
        Assert.Contains("3", button.TextContent);
        Assert.Contains("3 waiting", button.GetAttribute("aria-label"));
        Assert.DoesNotContain("never", button.GetAttribute("aria-label"));
    }

    [Fact]
    public void Updates_when_sync_state_changes()
    {
        var cut = Render<SyncStatusIndicator>();

        Sync.IsOnline = false;
        Sync.RaiseStateChanged();

        cut.WaitForAssertion(() => Assert.Contains("Offline", cut.Find("[data-testid=sync-indicator]").TextContent));
    }

    [Fact]
    public void Shows_sync_error()
    {
        Sync.LastError = "permission-denied";

        var cut = Render<SyncStatusIndicator>();

        Assert.Contains("Sync error", cut.Find("[data-testid=sync-indicator]").TextContent);
    }
}
