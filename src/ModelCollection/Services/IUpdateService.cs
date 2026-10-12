namespace ModelCollection.Services;

/// <summary>Detects when a new service worker (app version) is waiting.</summary>
public interface IUpdateService
{
    bool UpdateAvailable { get; }
    event Action? Changed;
    Task InitializeAsync();
    Task ApplyUpdateAsync();
}
