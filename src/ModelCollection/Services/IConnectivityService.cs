namespace ModelCollection.Services;

public interface IConnectivityService
{
    bool IsOnline { get; }

    /// <summary>Raised with the new online state.</summary>
    event Action<bool>? Changed;

    Task InitializeAsync();
}
