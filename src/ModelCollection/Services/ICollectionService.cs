using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>Tracks which shared cloud collection the signed-in user belongs to.</summary>
public interface ICollectionService
{
    SharedCollection? Current { get; }

    /// <summary>True while looking up the user's collection after sign-in.</summary>
    bool IsLoading { get; }

    event Action? Changed;

    Task InitializeAsync();
    Task CreateAsync(string name);
    Task AddMemberAsync(string email);
    Task RemoveMemberAsync(string email);
}
