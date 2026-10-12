using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class LocalStore(IIndexedDb db, TimeProvider clock) : ILocalStore
{
    private sealed record MetaEntry(string Key, string Value);

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
        await DeletePhotosForCarAsync(id);
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
            await DeletePhotosForCarAsync(remote.Id);
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

    public Task<IReadOnlyList<CarPhoto>> GetAllPhotosAsync() => db.GetAllAsync<CarPhoto>(Stores.Photos);

    public Task<IReadOnlyList<CarPhoto>> GetAllThumbnailsAsync() => db.GetAllAsync<CarPhoto>(Stores.Thumbnails);

    public async Task AddPhotoAsync(CarPhoto photo, CarPhoto thumbnail)
    {
        await db.PutAsync(Stores.Photos, photo);
        await db.PutAsync(Stores.Thumbnails, thumbnail);
        Changed?.Invoke();
    }

    public async Task DeletePhotoAsync(string photoId)
    {
        await db.DeleteAsync(Stores.Photos, photoId);
        await db.DeleteAsync(Stores.Thumbnails, photoId);
        Changed?.Invoke();
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

    private async Task DeletePhotosForCarAsync(string carId)
    {
        foreach (var photo in await db.GetAllByIndexAsync<CarPhoto>(Stores.Photos, Stores.CarIdIndex, carId))
        {
            await db.DeleteAsync(Stores.Photos, photo.Id);
            await db.DeleteAsync(Stores.Thumbnails, photo.Id);
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
