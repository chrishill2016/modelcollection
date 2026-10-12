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
}
