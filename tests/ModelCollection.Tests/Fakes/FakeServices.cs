using ModelCollection.Models;
using ModelCollection.Services;

namespace ModelCollection.Tests.Fakes;

public sealed class FakeConnectivity : IConnectivityService
{
    public bool IsOnline { get; private set; } = true;
    public event Action<bool>? Changed;
    public Task InitializeAsync() => Task.CompletedTask;

    public void Set(bool online)
    {
        IsOnline = online;
        Changed?.Invoke(online);
    }
}

public sealed class FakeAuth : IAuthService
{
    public bool IsConfigured { get; set; } = true;
    public bool IsReady { get; set; } = true;
    public AppUser? User { get; set; } = new("uid-1", "dad@example.com", "Dad", null);
    public event Action? Changed;
    public Task InitializeAsync() => Task.CompletedTask;

    public Task SignInAsync()
    {
        User = new("uid-1", "dad@example.com", "Dad", null);
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        User = null;
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    public void RaiseChanged() => Changed?.Invoke();
}

public sealed class FakeCollections : ICollectionService
{
    public SharedCollection? Current { get; set; } = new("col-1", "Dad's cars", ["dad@example.com", "chris@example.com"]);
    public bool IsLoading { get; set; }
    public event Action? Changed;
    public Task InitializeAsync() => Task.CompletedTask;
    public Task CreateAsync(string name) => Task.CompletedTask;
    public Task AddMemberAsync(string email) => Task.CompletedTask;
    public Task RemoveMemberAsync(string email) => Task.CompletedTask;
    public void RaiseChanged() => Changed?.Invoke();
}
