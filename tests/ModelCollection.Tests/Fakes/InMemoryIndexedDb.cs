using System.Text.Json;
using ModelCollection.Services;

namespace ModelCollection.Tests.Fakes;

/// <summary>IndexedDB stand-in that round-trips values through camelCase JSON like the real interop.</summary>
public sealed class InMemoryIndexedDb : IIndexedDb
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, string> KeyPaths = new()
    {
        [Stores.Cars] = "id",
        [Stores.Photos] = "id",
        [Stores.Thumbnails] = "id",
        [Stores.Outbox] = "carId",
        [Stores.Meta] = "key",
        [Stores.PhotoOutbox] = "photoId",
    };

    private readonly Dictionary<string, Dictionary<string, string>> _stores = KeyPaths.Keys.ToDictionary(k => k, _ => new Dictionary<string, string>());

    public Task<T?> GetAsync<T>(string store, string key) =>
        Task.FromResult(_stores[store].TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json, Json) : default);

    public Task<IReadOnlyList<T>> GetAllAsync<T>(string store) =>
        Task.FromResult<IReadOnlyList<T>>(_stores[store].Values.Select(v => JsonSerializer.Deserialize<T>(v, Json)!).ToList());

    public Task<IReadOnlyList<T>> GetAllByIndexAsync<T>(string store, string index, string value) =>
        Task.FromResult<IReadOnlyList<T>>(_stores[store].Values
            .Where(v => JsonDocument.Parse(v).RootElement.GetProperty(index).GetString() == value)
            .Select(v => JsonSerializer.Deserialize<T>(v, Json)!)
            .ToList());

    public Task PutAsync<T>(string store, T value)
    {
        var json = JsonSerializer.Serialize(value, Json);
        var key = JsonDocument.Parse(json).RootElement.GetProperty(KeyPaths[store]).GetString()!;
        _stores[store][key] = json;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string store, string key)
    {
        _stores[store].Remove(key);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(string store) => Task.FromResult(_stores[store].Count);
}
