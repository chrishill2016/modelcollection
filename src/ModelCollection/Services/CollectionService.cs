using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class CollectionService(
    IAuthService auth,
    IRemoteStore remote,
    IConnectivityService connectivity,
    ILocalStore local,
    ILogger<CollectionService> logger) : ICollectionService
{
    private const string CollectionMetaKey = "collection";
    private string? _loadedForEmail;

    public SharedCollection? Current { get; private set; }
    public bool IsLoading { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        auth.Changed += () => _ = RefreshAsync();
        connectivity.Changed += online => { if (online) _ = RefreshAsync(); };
        await RefreshAsync();
    }

    public async Task CreateAsync(string name)
    {
        var email = RequireEmail();
        await SetCurrentAsync(await remote.CreateCollectionAsync(name.Trim(), email));
    }

    public async Task AddMemberAsync(string email)
    {
        var current = Current ?? throw new InvalidOperationException("No collection");
        email = NormaliseEmail(email);
        if (current.MemberEmails.Contains(email))
        {
            return;
        }

        var members = current.MemberEmails.Append(email).ToList();
        await remote.UpdateMembersAsync(current.Id, members);
        await SetCurrentAsync(current with { MemberEmails = members });
    }

    public async Task RemoveMemberAsync(string email)
    {
        var current = Current ?? throw new InvalidOperationException("No collection");
        if (string.Equals(email, auth.User?.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You can't remove yourself.");
        }

        var members = current.MemberEmails.Where(m => m != email).ToList();
        await remote.UpdateMembersAsync(current.Id, members);
        await SetCurrentAsync(current with { MemberEmails = members });
    }

    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task RefreshAsync()
    {
        var email = auth.User?.Email is { } e ? NormaliseEmail(e) : null;
        if (email is null)
        {
            _loadedForEmail = null;
            if (Current is not null)
            {
                Current = null;
                Changed?.Invoke();
            }
            return;
        }

        if (_loadedForEmail == email && Current is not null)
        {
            return;
        }

        IsLoading = true;
        Changed?.Invoke();
        try
        {
            // Use the last known collection straight away so the app works offline.
            if (Current is null && await local.GetMetaAsync(CollectionMetaKey) is { } cached
                && System.Text.Json.JsonSerializer.Deserialize<SharedCollection>(cached) is { } c
                && c.MemberEmails.Contains(email))
            {
                Current = c;
            }

            if (connectivity.IsOnline)
            {
                var found = await remote.FindCollectionAsync(email);
                _loadedForEmail = email;
                await SetCurrentAsync(found);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not look up collection");
        }
        finally
        {
            IsLoading = false;
            Changed?.Invoke();
        }
    }

    private async Task SetCurrentAsync(SharedCollection? collection)
    {
        Current = collection;
        await local.SetMetaAsync(CollectionMetaKey, collection is null ? null : System.Text.Json.JsonSerializer.Serialize(collection));
        Changed?.Invoke();
    }

    private string RequireEmail() =>
        auth.User?.Email is { } email ? NormaliseEmail(email) : throw new InvalidOperationException("Sign in first");
}
