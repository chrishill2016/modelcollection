using ModelCollection.Models;

namespace ModelCollection.Services;

public interface IAuthService
{
    /// <summary>False when no Firebase config was supplied; the app then runs local-only.</summary>
    bool IsConfigured { get; }

    /// <summary>True once the signed-in state is known (Firebase restores sessions asynchronously).</summary>
    bool IsReady { get; }

    AppUser? User { get; }

    event Action? Changed;

    Task InitializeAsync();
    Task SignInAsync();
    Task SignOutAsync();
}
