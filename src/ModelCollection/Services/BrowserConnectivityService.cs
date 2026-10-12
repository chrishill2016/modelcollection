using Microsoft.JSInterop;

namespace ModelCollection.Services;

public sealed class BrowserConnectivityService(BrowserInterop browser) : IConnectivityService, IDisposable
{
    private DotNetObjectReference<BrowserConnectivityService>? _self;

    public bool IsOnline { get; private set; } = true;

    public event Action<bool>? Changed;

    public async Task InitializeAsync()
    {
        _self = DotNetObjectReference.Create(this);
        IsOnline = await browser.InvokeAsync<bool>("watchConnectivity", _self);
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        if (IsOnline == isOnline)
        {
            return;
        }

        IsOnline = isOnline;
        Changed?.Invoke(isOnline);
    }

    public void Dispose() => _self?.Dispose();
}
