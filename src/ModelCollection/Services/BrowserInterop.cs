using Microsoft.JSInterop;

namespace ModelCollection.Services;

/// <summary>Loads js/browser.js: connectivity, image resizing, file downloads and app updates.</summary>
public sealed class BrowserInterop(IJSRuntime js) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _module;

    private Task<IJSObjectReference> ModuleAsync() =>
        _module ??= js.InvokeAsync<IJSObjectReference>("import", "./js/browser.js").AsTask();

    public async Task<T> InvokeAsync<T>(string identifier, params object?[] args) =>
        await (await ModuleAsync()).InvokeAsync<T>(identifier, args);

    public async Task InvokeVoidAsync(string identifier, params object?[] args) =>
        await (await ModuleAsync()).InvokeVoidAsync(identifier, args);

    public async ValueTask DisposeAsync()
    {
        if (_module is { IsCompletedSuccessfully: true })
        {
            await _module.Result.DisposeAsync();
        }
    }
}
