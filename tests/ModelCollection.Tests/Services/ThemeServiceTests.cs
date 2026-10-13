using Microsoft.Extensions.Logging.Abstractions;

namespace ModelCollection.Tests.Services;

public class ThemeServiceTests
{
    private readonly LocalStore _local = new(new InMemoryIndexedDb(), new FakeTimeProvider());

    private ThemeService Create() => new(_local, new BrowserInterop(null!), NullLogger<ThemeService>.Instance);

    [Fact]
    public async Task Defaults_to_light()
    {
        var themes = Create();
        await themes.InitializeAsync();

        Assert.Equal(ThemePreference.Light, themes.Preference);
        Assert.False(themes.IsDark(systemIsDark: true));
    }

    [Fact]
    public async Task Choice_is_remembered()
    {
        await Create().SetAsync(ThemePreference.Dark);

        var restarted = Create();
        await restarted.InitializeAsync();

        Assert.Equal(ThemePreference.Dark, restarted.Preference);
        Assert.True(restarted.IsDark(systemIsDark: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Match_phone_follows_the_device(bool systemIsDark)
    {
        var themes = Create();
        await themes.SetAsync(ThemePreference.System);

        Assert.Equal(systemIsDark, themes.IsDark(systemIsDark));
    }

    [Fact]
    public async Task Changing_the_theme_raises_Changed()
    {
        var themes = Create();
        var raised = 0;
        themes.Changed += () => raised++;

        await themes.SetAsync(ThemePreference.Dark);

        Assert.Equal(1, raised);
    }
}
