using Bunit.TestDoubles;
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
        Services.AddSingleton<IPhotoService, NoPhotoService>();
        Services.AddSingleton<IRemoteStore>(new FakeRemoteStore { SupportsPhotos = false });
        Services.AddSingleton<BrowserInterop>();
        Services.AddSingleton<AppNavigation>();
    }

    protected IRenderedComponent<T> Render<T>(Action<ComponentParameterCollectionBuilder<T>>? parameters = null)
        where T : IComponent => _context.Render(parameters);

    protected BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

    protected BunitJSInterop JSInterop => _context.JSInterop;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();
}

internal sealed class NoPhotoService : IPhotoService
{
    public Task<CarPhoto> AddPhotoAsync(string carId, Microsoft.AspNetCore.Components.Forms.IBrowserFile file) =>
        throw new NotSupportedException();

    public Task<string?> GetFullImageAsync(CarPhoto thumbnail) => Task.FromResult<string?>(null);
}
