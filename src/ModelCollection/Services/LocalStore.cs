using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class LocalStore(IIndexedDb db, TimeProvider clock) : ILocalStore
{
    private sealed record MetaEntry(string Key, string Value);

    internal const string ExistingPhotosQueuedKey = "existingPhotosQueued";

    public event Action? Changed;

    public async Task<IReadOnlyList<ModelCar>> GetCarsAsync() =>
        (await db.GetAllAsync<ModelCar>(Stores.Cars)).Where(c => !c.IsDeleted).ToList();

    public Task<ModelCar?> GetCarAsync(string id) => db.GetAsync<ModelCar>(Stores.Cars, id);

    public async Task<ModelCar> SaveCarAsync(ModelCar car)
    {
        var existing = await GetCarAsync(car.Id);
        var saved = car with { LastModified = NextTimestamp(existing) };
        await db.PutAsync(Stores.Cars, saved);
        await QueueAsync(saved.Id);
        Changed?.Invoke();
        return saved;
    }

    public async Task DeleteCarAsync(string id)
    {
        var existing = await GetCarAsync(id);
        if (existing is null || existing.IsDeleted)
        {
            return;
        }

        await db.PutAsync(Stores.Cars, existing with { IsDeleted = true, LastModified = NextTimestamp(existing) });
        await QueueAsync(id);
        await DeletePhotosForCarAsync(id, queue: true);
        Changed?.Invoke();
    }

    public async Task<bool> ApplyRemoteAsync(ModelCar remote)
    {
        var local = await GetCarAsync(remote.Id);
        if (local is not null && local.LastModified >= remote.LastModified)
        {
            return false;
        }

        await db.PutAsync(Stores.Cars, remote);
        // The cloud copy is newer, so any pending local edit has lost.
        await db.DeleteAsync(Stores.Outbox, remote.Id);
        if (remote.IsDeleted)
        {
            // The device that deleted the car queued its photo deletions.
            await DeletePhotosForCarAsync(remote.Id, queue: false);
        }

        Changed?.Invoke();
        return true;
    }

    public async Task<bool> ImportCarAsync(ModelCar car)
    {
        var local = await GetCarAsync(car.Id);
        if (local is not null && local.LastModified >= car.LastModified)
        {
            return false;
        }

        await db.PutAsync(Stores.Cars, car);
        await QueueAsync(car.Id);
        Changed?.Invoke();
        return true;
    }

    public Task<IReadOnlyList<OutboxEntry>> GetOutboxAsync() => db.GetAllAsync<OutboxEntry>(Stores.Outbox);

    public Task<int> GetPendingCountAsync() => db.CountAsync(Stores.Outbox);

    public async Task CompleteOutboxAsync(OutboxEntry entry)
    {
        var current = await db.GetAsync<OutboxEntry>(Stores.Outbox, entry.CarId);
        if (current is not null && current.Queued == entry.Queued)
        {
            await db.DeleteAsync(Stores.Outbox, entry.CarId);
            Changed?.Invoke();
        }
    }

    public async Task<IReadOnlyList<CarPhoto>> GetPhotosAsync(string carId) =>
        (await db.GetAllByIndexAsync<CarPhoto>(Stores.Photos, Stores.CarIdIndex, carId)).OrderBy(p => p.Created).ToList();

    public async Task<IReadOnlyList<CarPhoto>> GetThumbnailsAsync(string carId) =>
        (await db.GetAllByIndexAsync<CarPhoto>(Stores.Thumbnails, Stores.CarIdIndex, carId)).OrderBy(p => p.Created).ToList();

    public Task<CarPhoto?> GetPhotoAsync(string photoId) => db.GetAsync<CarPhoto>(Stores.Photos, photoId);

    public Task<CarPhoto?> GetThumbnailAsync(string photoId) => db.GetAsync<CarPhoto>(Stores.Thumbnails, photoId);

    public Task<IReadOnlyList<CarPhoto>> GetAllPhotosAsync() => db.GetAllAsync<CarPhoto>(Stores.Photos);

    public Task<IReadOnlyList<CarPhoto>> GetAllThumbnailsAsync() => db.GetAllAsync<CarPhoto>(Stores.Thumbnails);

    public async Task AddPhotoAsync(CarPhoto photo, CarPhoto thumbnail)
    {
        await db.PutAsync(Stores.Photos, photo);
        await db.PutAsync(Stores.Thumbnails, thumbnail);
        await QueuePhotoAsync(photo.Id, photo.CarId, PhotoOp.Upload);
        Changed?.Invoke();
    }

    public async Task DeletePhotoAsync(string photoId)
    {
        var thumbnail = await GetThumbnailAsync(photoId);
        var carId = thumbnail?.CarId ?? (await GetPhotoAsync(photoId))?.CarId;
        await db.DeleteAsync(Stores.Photos, photoId);
        await db.DeleteAsync(Stores.Thumbnails, photoId);
        if (carId is not null)
        {
            await QueuePhotoAsync(photoId, carId, PhotoOp.Delete);
        }

        Changed?.Invoke();
    }

    public async Task SaveDownloadedThumbnailAsync(CarPhoto thumbnail)
    {
        await db.PutAsync(Stores.Thumbnails, thumbnail);
        Changed?.Invoke();
    }

    public async Task SaveDownloadedPhotoAsync(CarPhoto photo)
    {
        // Not raising Changed: a full image arriving doesn't change what lists or counts show.
        await db.PutAsync(Stores.Photos, photo);
    }

    public async Task RemoveSyncedPhotoAsync(string photoId)
    {
        await db.DeleteAsync(Stores.Photos, photoId);
        await db.DeleteAsync(Stores.Thumbnails, photoId);
        await db.DeleteAsync(Stores.PhotoOutbox, photoId);
        Changed?.Invoke();
    }

    public Task<IReadOnlyList<PhotoOutboxEntry>> GetPhotoOutboxAsync() => db.GetAllAsync<PhotoOutboxEntry>(Stores.PhotoOutbox);

    public Task<int> GetPendingPhotoCountAsync() => db.CountAsync(Stores.PhotoOutbox);

    public async Task CompletePhotoOutboxAsync(PhotoOutboxEntry entry)
    {
        var current = await db.GetAsync<PhotoOutboxEntry>(Stores.PhotoOutbox, entry.PhotoId);
        if (current is not null && current.Queued == entry.Queued && current.Op == entry.Op)
        {
            await db.DeleteAsync(Stores.PhotoOutbox, entry.PhotoId);
            Changed?.Invoke();
        }
    }

    public async Task QueueExistingPhotosOnceAsync()
    {
        if (await GetMetaAsync(ExistingPhotosQueuedKey) is not null)
        {
            return;
        }

        var queued = (await GetPhotoOutboxAsync()).Select(e => e.PhotoId).ToHashSet();
        foreach (var photo in await GetAllPhotosAsync())
        {
            if (!queued.Contains(photo.Id))
            {
                await QueuePhotoAsync(photo.Id, photo.CarId, PhotoOp.Upload);
            }
        }

        await SetMetaAsync(ExistingPhotosQueuedKey, "1");
    }

    public async Task<string?> GetMetaAsync(string key) => (await db.GetAsync<MetaEntry>(Stores.Meta, key))?.Value;

    public async Task SetMetaAsync(string key, string? value)
    {
        if (value is null)
        {
            await db.DeleteAsync(Stores.Meta, key);
        }
        else
        {
            await db.PutAsync(Stores.Meta, new MetaEntry(key, value));
        }
    }

    private async Task QueueAsync(string carId) =>
        await db.PutAsync(Stores.Outbox, new OutboxEntry(carId, clock.GetUtcNow()));

    private async Task QueuePhotoAsync(string photoId, string carId, PhotoOp op) =>
        await db.PutAsync(Stores.PhotoOutbox, new PhotoOutboxEntry(photoId, carId, op, clock.GetUtcNow()));

    private async Task DeletePhotosForCarAsync(string carId, bool queue)
    {
        // Every photo has a thumbnail; synced photos may not have their full image downloaded.
        foreach (var photo in await GetThumbnailsAsync(carId))
        {
            await db.DeleteAsync(Stores.Photos, photo.Id);
            await db.DeleteAsync(Stores.Thumbnails, photo.Id);
            if (queue)
            {
                await QueuePhotoAsync(photo.Id, carId, PhotoOp.Delete);
            }
            else
            {
                await db.DeleteAsync(Stores.PhotoOutbox, photo.Id);
            }
        }
    }

    // Keeps timestamps moving forward even if this device's clock is behind the last edit.
    private DateTimeOffset NextTimestamp(ModelCar? existing)
    {
        var now = clock.GetUtcNow();
        return existing is not null && existing.LastModified >= now
            ? existing.LastModified.AddMilliseconds(1)
            : now;
    }
}
