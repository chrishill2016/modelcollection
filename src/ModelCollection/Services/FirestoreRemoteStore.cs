using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class FirestoreRemoteStore(FirebaseInterop firebase) : IRemoteStore
{
    public Task<SharedCollection?> FindCollectionAsync(string email) =>
        firebase.InvokeAsync<SharedCollection?>("findCollection", email);

    public Task<SharedCollection> CreateCollectionAsync(string name, string ownerEmail) =>
        firebase.InvokeAsync<SharedCollection>("createCollection", name, ownerEmail);

    public Task UpdateMembersAsync(string collectionId, IReadOnlyList<string> memberEmails) =>
        firebase.InvokeVoidAsync("updateMembers", collectionId, memberEmails);

    public Task<RemotePullResult> PullAsync(string collectionId, long cursor) =>
        firebase.InvokeAsync<RemotePullResult>("pullCars", collectionId, cursor);

    public bool SupportsPhotos => firebase.Options.HasStorage;

    public Task<RemotePhotoPullResult> PullPhotosAsync(string collectionId, long cursor) =>
        firebase.InvokeAsync<RemotePhotoPullResult>("pullPhotos", collectionId, cursor);

    public Task UploadPhotoAsync(string collectionId, CarPhoto photo, CarPhoto thumbnail) =>
        firebase.InvokeVoidAsync("uploadPhoto", collectionId,
            new RemotePhoto(photo.Id, photo.CarId, photo.Created, IsDeleted: false), photo.DataUrl, thumbnail.DataUrl);

    public Task DeletePhotoAsync(string collectionId, string photoId, string carId) =>
        firebase.InvokeVoidAsync("deletePhoto", collectionId, photoId, carId);

    public Task<string> DownloadPhotoAsync(string collectionId, string photoId, bool thumbnail) =>
        firebase.InvokeAsync<string>("downloadPhoto", collectionId, photoId, thumbnail);

    public async Task PushAsync(string collectionId, IReadOnlyList<ModelCar> cars)
    {
        // Firestore batches are limited to 500 writes.
        foreach (var chunk in cars.Chunk(400))
        {
            await firebase.InvokeVoidAsync("pushCars", collectionId, chunk);
        }
    }
}
