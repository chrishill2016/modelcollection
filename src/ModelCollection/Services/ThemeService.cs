namespace ModelCollection.Services;

public enum ThemePreference
{
    Light,
    Dark,
    /// <summary>Follow the device's light/dark setting.</summary>
    System,
}

public interface IThemeService
{
    ThemePreference Preference { get; }
    event Action? Changed;
    Task InitializeAsync();
    Task SetAsync(ThemePreference preference);

    /// <summary>Whether to show the dark theme, given whether the device itself is in dark mode.</summary>
    bool IsDark(bool systemIsDark);
}

/// <summary>
/// Remembers the theme on this device. Light is the default. The choice is also mirrored to the page
/// (status-bar colour and loading screen) so the app doesn't flash the wrong theme while starting.
/// </summary>
public sealed class ThemeService(ILocalStore local, BrowserInterop browser, ILogger<ThemeService> logger) : IThemeService
{
    internal const string MetaKey = "theme";

    public ThemePreference Preference { get; private set; } = ThemePreference.Light;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (Enum.TryParse<ThemePreference>(await local.GetMetaAsync(MetaKey), out var saved))
        {
            Preference = saved;
        }
    }

    public async Task SetAsync(ThemePreference preference)
    {
        Preference = preference;
        await local.SetMetaAsync(MetaKey, preference.ToString());
        Changed?.Invoke();
    }

    public bool IsDark(bool systemIsDark) => Preference switch
    {
        ThemePreference.Dark => true,
        ThemePreference.System => systemIsDark,
        _ => false,
    };

    /// <summary>Updates the browser chrome to match (best effort; not available in tests or prerender).</summary>
    public async Task ApplyToPageAsync(bool isDark)
    {
        try
        {
            await browser.InvokeVoidAsync("applyTheme", Preference.ToString().ToLowerInvariant(), isDark);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not apply theme to page");
        }
    }
}
