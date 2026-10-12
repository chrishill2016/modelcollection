namespace ModelCollection.Services;

/// <summary>Thin wrapper over the browser's IndexedDB. Values are stored as camelCase JSON.</summary>
public interface IIndexedDb
{
    Task<T?> GetAsync<T>(string store, string key);
    Task<IReadOnlyList<T>> GetAllAsync<T>(string store);
    Task<IReadOnlyList<T>> GetAllByIndexAsync<T>(string store, string index, string value);
    Task PutAsync<T>(string store, T value);
    Task DeleteAsync(string store, string key);
    Task<int> CountAsync(string store);
}

public static class Stores
{
    public const string Cars = "cars";             // keyPath: id
    public const string Photos = "photos";         // keyPath: id, index: carId
    public const string Thumbnails = "thumbnails"; // keyPath: id, index: carId
    public const string Outbox = "outbox";         // keyPath: carId
    public const string Meta = "meta";             // keyPath: key

    public const string CarIdIndex = "carId";
}
