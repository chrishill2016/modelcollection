using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>The cloud database. Swap the implementation to change backend.</summary>
public interface IRemoteStore
{
    Task<SharedCollection?> FindCollectionAsync(string email);
    Task<SharedCollection> CreateCollectionAsync(string name, string ownerEmail);
    Task UpdateMembersAsync(string collectionId, IReadOnlyList<string> memberEmails);

    /// <summary>Cars changed since <paramref name="cursor"/> (0 = everything).</summary>
    Task<RemotePullResult> PullAsync(string collectionId, long cursor);

    Task PushAsync(string collectionId, IReadOnlyList<ModelCar> cars);

    /// <summary>False when no cloud storage is set up; photos then stay on each device.</summary>
    bool SupportsPhotos { get; }

    /// <summary>Photo metadata changed since <paramref name="cursor"/> (0 = everything).</summary>
    Task<RemotePhotoPullResult> PullPhotosAsync(string collectionId, long cursor);

    /// <summary>Uploads the full image and thumbnail, then records the photo's metadata.</summary>
    Task UploadPhotoAsync(string collectionId, CarPhoto photo, CarPhoto thumbnail);

    /// <summary>Marks the photo deleted (so other devices remove it) and deletes its files.</summary>
    Task DeletePhotoAsync(string collectionId, string photoId, string carId);

    /// <summary>Downloads a photo as a data URL.</summary>
    Task<string> DownloadPhotoAsync(string collectionId, string photoId, bool thumbnail);
}
