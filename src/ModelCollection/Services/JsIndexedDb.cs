using Microsoft.JSInterop;

namespace ModelCollection.Services;

public sealed class JsIndexedDb(IJSRuntime js) : IIndexedDb, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module =
        new(() => js.InvokeAsync<IJSObjectReference>("import", "./js/indexeddb.js").AsTask());

    private async Task<IJSObjectReference> Module() => await _module.Value;

    public async Task<T?> GetAsync<T>(string store, string key) =>
        await (await Module()).InvokeAsync<T?>("get", store, key);

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string store) =>
        await (await Module()).InvokeAsync<List<T>>("getAll", store);

    public async Task<IReadOnlyList<T>> GetAllByIndexAsync<T>(string store, string index, string value) =>
        await (await Module()).InvokeAsync<List<T>>("getAllByIndex", store, index, value);

    public async Task PutAsync<T>(string store, T value) =>
        await (await Module()).InvokeVoidAsync("put", store, value);

    public async Task DeleteAsync(string store, string key) =>
        await (await Module()).InvokeVoidAsync("remove", store, key);

    public async Task<int> CountAsync(string store) =>
        await (await Module()).InvokeAsync<int>("count", store);

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            await (await _module.Value).DisposeAsync();
        }
    }
}
