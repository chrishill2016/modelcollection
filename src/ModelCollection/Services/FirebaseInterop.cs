using Microsoft.JSInterop;
using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>Loads js/firebase.js, which wraps the vendored Firebase JS SDK.</summary>
public sealed class FirebaseInterop(IJSRuntime js, FirebaseOptions options) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _module;

    public FirebaseOptions Options => options;

    public Task<IJSObjectReference> ModuleAsync() =>
        _module ??= js.InvokeAsync<IJSObjectReference>("import", "./js/firebase.js").AsTask();

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
