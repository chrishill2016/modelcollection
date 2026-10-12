using Microsoft.JSInterop;
using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class FirebaseAuthService(FirebaseInterop firebase, ILogger<FirebaseAuthService> logger) : IAuthService, IDisposable
{
    private DotNetObjectReference<FirebaseAuthService>? _self;

    public bool IsConfigured => firebase.Options.IsConfigured;
    public bool IsReady { get; private set; }
    public AppUser? User { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (!IsConfigured)
        {
            IsReady = true;
            Changed?.Invoke();
            return;
        }

        try
        {
            _self = DotNetObjectReference.Create(this);
            await firebase.InvokeVoidAsync("init", firebase.Options, _self);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Firebase could not be initialised; running local-only");
            IsReady = true;
            Changed?.Invoke();
        }
    }

    public Task SignInAsync() => firebase.InvokeVoidAsync("signIn");

    public Task SignOutAsync() => firebase.InvokeVoidAsync("signOut");

    [JSInvokable]
    public void OnAuthChanged(AppUser? user)
    {
        User = user;
        IsReady = true;
        Changed?.Invoke();
    }

    public void Dispose() => _self?.Dispose();
}
