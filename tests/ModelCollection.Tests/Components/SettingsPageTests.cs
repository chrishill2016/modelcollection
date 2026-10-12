using Bunit;
using ModelCollection.Features.Settings;

namespace ModelCollection.Tests.Components;

public class SettingsPageTests : ComponentTestBase
{
    [Fact]
    public void Shows_app_version()
    {
        var cut = Render<SettingsPage>();

        Assert.Equal(AppInfo.Version, cut.Find("[data-testid=app-version]").TextContent);
    }

    [Fact]
    public void Explains_local_only_mode_when_cloud_is_not_configured()
    {
        Auth.IsConfigured = false;
        Auth.User = null;

        var cut = Render<SettingsPage>();

        Assert.Contains("saved on this device only", cut.Markup);
    }

    [Fact]
    public void Lists_collection_members()
    {
        var cut = Render<SettingsPage>();

        Assert.Contains("Dad's cars", cut.Markup);
        Assert.Contains("chris@example.com", cut.Markup);
        Assert.Contains("Remove chris@example.com", cut.Markup);
        Assert.DoesNotContain("Remove dad@example.com", cut.Markup);
    }

    [Fact]
    public void Offers_to_create_a_collection_when_not_in_one()
    {
        Collections.Current = null;

        var cut = Render<SettingsPage>();

        Assert.Contains("You're not part of a collection yet", cut.Markup);
    }
}
