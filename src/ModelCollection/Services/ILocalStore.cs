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

    /// <summary>Full-size photos stored on this device for a car.</summary>
    Task<IReadOnlyList<CarPhoto>> GetPhotosAsync(string carId);

    /// <summary>Thumbnails for a car, oldest first. Every photo has one, including those synced from elsewhere.</summary>
    Task<IReadOnlyList<CarPhoto>> GetThumbnailsAsync(string carId);

    Task<CarPhoto?> GetPhotoAsync(string photoId);
    Task<CarPhoto?> GetThumbnailAsync(string photoId);
    Task<IReadOnlyList<CarPhoto>> GetAllPhotosAsync();
    Task<IReadOnlyList<CarPhoto>> GetAllThumbnailsAsync();

    /// <summary>Adds a photo taken or imported on this device and queues it for upload.</summary>
    Task AddPhotoAsync(CarPhoto photo, CarPhoto thumbnail);

    /// <summary>Deletes a photo on this device and queues the deletion for other devices.</summary>
    Task DeletePhotoAsync(string photoId);

    /// <summary>Stores a thumbnail or full image downloaded from the cloud (not queued for upload).</summary>
    Task SaveDownloadedThumbnailAsync(CarPhoto thumbnail);
    Task SaveDownloadedPhotoAsync(CarPhoto photo);

    /// <summary>Removes a photo that was deleted on another device.</summary>
    Task RemoveSyncedPhotoAsync(string photoId);

    Task<IReadOnlyList<PhotoOutboxEntry>> GetPhotoOutboxAsync();
    Task<int> GetPendingPhotoCountAsync();

    /// <summary>Removes a photo outbox entry once pushed, unless it was re-queued since.</summary>
    Task CompletePhotoOutboxAsync(PhotoOutboxEntry entry);

    /// <summary>Queues every photo already on this device for upload, once (photos taken before sync existed).</summary>
    Task QueueExistingPhotosOnceAsync();

    Task<string?> GetMetaAsync(string key);
    Task SetMetaAsync(string key, string? value);
}
