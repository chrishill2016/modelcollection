using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ModelCollection.Features.Collection;
using MudBlazor.Services;

namespace ModelCollection.Tests.Components;

/// <summary>
/// Wraps a bUnit context. MudBlazor's services are async-disposable only, and xUnit v2 disposes test
/// classes synchronously, so the context is disposed via <see cref="IAsyncLifetime"/> instead.
/// </summary>
public abstract class ComponentTestBase : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    protected FakeTimeProvider Clock { get; } = new(DateTimeOffset.Parse("2026-05-01T10:00:00Z"));
    protected LocalStore Store { get; }
    protected FakeSyncService Sync { get; } = new();
    protected FakeAuth Auth { get; } = new();
    protected FakeCollections Collections { get; } = new();
    protected BunitServiceProvider Services => _context.Services;

    protected ComponentTestBase()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Store = new LocalStore(new InMemoryIndexedDb(), Clock);
        Services.AddSingleton<ILocalStore>(Store);
        Services.AddSingleton<ISyncService>(Sync);
        Services.AddSingleton<IAuthService>(Auth);
        Services.AddSingleton<ICollectionService>(Collections);
        Services.AddSingleton<CollectionViewState>();
        Services.AddSingleton<TimeProvider>(Clock);
    }

    protected IRenderedComponent<T> Render<T>(Action<ComponentParameterCollectionBuilder<T>>? parameters = null)
        where T : IComponent => _context.Render(parameters);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();
}
