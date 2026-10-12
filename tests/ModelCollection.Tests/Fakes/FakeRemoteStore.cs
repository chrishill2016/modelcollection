using ModelCollection.Models;
using ModelCollection.Services;

namespace ModelCollection.Tests.Fakes;

/// <summary>In-memory Firestore stand-in with a server clock for pull cursors.</summary>
public sealed class FakeRemoteStore : IRemoteStore
{
    private long _serverClock = 1000;

    public Dictionary<string, SharedCollection> Collections { get; } = [];
    public Dictionary<string, (ModelCar Car, long ServerUpdatedAt)> Cars { get; } = [];
    public Dictionary<string, (RemotePhoto Photo, long ServerUpdatedAt)> Photos { get; } = [];

    /// <summary>Stored images, keyed "photos/{id}" or "thumbs/{id}".</summary>
    public Dictionary<string, string> Files { get; } = [];

    public bool SupportsPhotos { get; set; } = true;
    public int Downloads { get; private set; }

    /// <summary>Simulates another device uploading a photo.</summary>
    public void UploadFromOtherDevice(string photoId, string carId, DateTimeOffset created)
    {
        Files[$"photos/{photoId}"] = $"data:full-{photoId}";
        Files[$"thumbs/{photoId}"] = $"data:thumb-{photoId}";
        Photos[photoId] = (new RemotePhoto(photoId, carId, created, false), ++_serverClock);
    }

    /// <summary>Simulates another device deleting a photo.</summary>
    public void DeleteFromOtherDevice(string photoId)
    {
        var existing = Photos[photoId].Photo;
        Photos[photoId] = (existing with { IsDeleted = true }, ++_serverClock);
        Files.Remove($"photos/{photoId}");
        Files.Remove($"thumbs/{photoId}");
    }
    public List<IReadOnlyList<ModelCar>> Pushes { get; } = [];
    public Exception? FailWith { get; set; }

    /// <summary>Runs during a push, e.g. to simulate a user editing mid-sync.</summary>
    public Func<Task>? DuringPush { get; set; }

    /// <summary>Simulates another device writing to the cloud.</summary>
    public void WriteFromOtherDevice(ModelCar car) => Cars[car.Id] = (car, ++_serverClock);

    public Task<SharedCollection?> FindCollectionAsync(string email) =>
        Task.FromResult(Collections.Values.FirstOrDefault(c => c.MemberEmails.Contains(email)));

    public Task<SharedCollection> CreateCollectionAsync(string name, string ownerEmail)
    {
        var c = new SharedCollection(Guid.NewGuid().ToString("N"), name, [ownerEmail]);
        Collections[c.Id] = c;
        return Task.FromResult(c);
    }

    public Task UpdateMembersAsync(string collectionId, IReadOnlyList<string> memberEmails)
    {
        Collections[collectionId] = Collections[collectionId] with { MemberEmails = memberEmails };
        return Task.CompletedTask;
    }

    public Task<RemotePullResult> PullAsync(string collectionId, long cursor)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        var changed = Cars.Values.Where(v => v.ServerUpdatedAt >= cursor).OrderBy(v => v.ServerUpdatedAt).ToList();
        var next = changed.Count == 0 ? cursor : changed.Max(v => v.ServerUpdatedAt);
        return Task.FromResult(new RemotePullResult(changed.Select(v => v.Car).ToList(), next));
    }

    public Task<RemotePhotoPullResult> PullPhotosAsync(string collectionId, long cursor)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        var changed = Photos.Values.Where(v => v.ServerUpdatedAt >= cursor).OrderBy(v => v.ServerUpdatedAt).ToList();
        var next = changed.Count == 0 ? cursor : changed.Max(v => v.ServerUpdatedAt);
        return Task.FromResult(new RemotePhotoPullResult(changed.Select(v => v.Photo).ToList(), next));
    }

    public Task UploadPhotoAsync(string collectionId, CarPhoto photo, CarPhoto thumbnail)
    {
        Files[$"photos/{photo.Id}"] = photo.DataUrl;
        Files[$"thumbs/{photo.Id}"] = thumbnail.DataUrl;
        Photos[photo.Id] = (new RemotePhoto(photo.Id, photo.CarId, photo.Created, false), ++_serverClock);
        return Task.CompletedTask;
    }

    public Task DeletePhotoAsync(string collectionId, string photoId, string carId)
    {
        Photos[photoId] = (new RemotePhoto(photoId, carId, DateTimeOffset.UnixEpoch, true), ++_serverClock);
        Files.Remove($"photos/{photoId}");
        Files.Remove($"thumbs/{photoId}");
        return Task.CompletedTask;
    }

    /// <summary>Simulates downloads being blocked (e.g. storage CORS not configured).</summary>
    public bool FailDownloads { get; set; }

    public Task<string> DownloadPhotoAsync(string collectionId, string photoId, bool thumbnail)
    {
        if (FailDownloads)
        {
            throw new InvalidOperationException("storage/unknown");
        }

        Downloads++;
        return Task.FromResult(Files[$"{(thumbnail ? "thumbs" : "photos")}/{photoId}"]);
    }

    public async Task PushAsync(string collectionId, IReadOnlyList<ModelCar> cars)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        if (DuringPush is { } hook)
        {
            DuringPush = null;
            await hook();
        }

        Pushes.Add(cars);
        foreach (var car in cars)
        {
            Cars[car.Id] = (car, ++_serverClock);
        }
    }
}
