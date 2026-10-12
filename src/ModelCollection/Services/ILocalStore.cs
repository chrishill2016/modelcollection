using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>The app's offline-first data store. All reads and writes go here first.</summary>
public interface ILocalStore
{
    /// <summary>Raised whenever cars, photos or the outbox change.</summary>
    event Action? Changed;

    /// <summary>All cars that have not been deleted.</summary>
    Task<IReadOnlyList<ModelCar>> GetCarsAsync();

    /// <summary>A car by id, including soft-deleted cars.</summary>
    Task<ModelCar?> GetCarAsync(string id);

    /// <summary>Saves a local edit: stamps <see cref="ModelCar.LastModified"/> and queues it for sync.</summary>
    Task<ModelCar> SaveCarAsync(ModelCar car);

    /// <summary>Soft-deletes a car (so the deletion syncs) and removes its photos from this device.</summary>
    Task DeleteCarAsync(string id);

    /// <summary>Applies a car from the cloud using last-write-wins. Returns true if the local copy changed.</summary>
    Task<bool> ApplyRemoteAsync(ModelCar remote);

    /// <summary>Imports a car from a backup using last-write-wins, queueing it for sync if applied.</summary>
    Task<bool> ImportCarAsync(ModelCar car);

    Task<IReadOnlyList<OutboxEntry>> GetOutboxAsync();
    Task<int> GetPendingCountAsync();

    /// <summary>Removes an outbox entry once pushed, unless the car was edited again since it was queued.</summary>
    Task CompleteOutboxAsync(OutboxEntry entry);

    Task<IReadOnlyList<CarPhoto>> GetPhotosAsync(string carId);
    Task<IReadOnlyList<CarPhoto>> GetAllPhotosAsync();
    Task<IReadOnlyList<CarPhoto>> GetAllThumbnailsAsync();
    Task AddPhotoAsync(CarPhoto photo, CarPhoto thumbnail);
    Task DeletePhotoAsync(string photoId);

    Task<string?> GetMetaAsync(string key);
    Task SetMetaAsync(string key, string? value);
}
