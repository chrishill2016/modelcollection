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

    public async Task PushAsync(string collectionId, IReadOnlyList<ModelCar> cars)
    {
        // Firestore batches are limited to 500 writes.
        foreach (var chunk in cars.Chunk(400))
        {
            await firebase.InvokeVoidAsync("pushCars", collectionId, chunk);
        }
    }
}
