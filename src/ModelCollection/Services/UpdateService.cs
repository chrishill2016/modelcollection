using Microsoft.JSInterop;

namespace ModelCollection.Services;

public sealed class UpdateService(BrowserInterop browser) : IUpdateService, IDisposable
{
    private DotNetObjectReference<UpdateService>? _self;

    public bool UpdateAvailable { get; private set; }
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        _self = DotNetObjectReference.Create(this);
        await browser.InvokeVoidAsync("watchForUpdates", _self);
    }

    public Task ApplyUpdateAsync() => browser.InvokeVoidAsync("applyUpdate");

    [JSInvokable]
    public void OnUpdateAvailable()
    {
        UpdateAvailable = true;
        Changed?.Invoke();
    }

    public void Dispose() => _self?.Dispose();
}
