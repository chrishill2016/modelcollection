using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ModelCollection.Services;

/// <summary>Knows whether the user has moved between pages in this session, so "back" can use browser history.</summary>
public sealed class AppNavigation : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly BrowserInterop _browser;

    public AppNavigation(NavigationManager nav, BrowserInterop browser)
    {
        _nav = nav;
        _browser = browser;
        _nav.LocationChanged += OnLocationChanged;
    }

    public bool HasHistory { get; private set; }

    /// <summary>Goes back a page, or to <paramref name="fallback"/> if the app was opened on this page.</summary>
    public async Task BackAsync(string fallback)
    {
        if (HasHistory)
        {
            await _browser.InvokeVoidAsync("goBack");
        }
        else
        {
            _nav.NavigateTo(fallback, replace: true);
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => HasHistory = true;

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
