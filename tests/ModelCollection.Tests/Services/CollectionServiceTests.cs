using Microsoft.Extensions.Logging.Abstractions;

namespace ModelCollection.Tests.Services;

public class CollectionServiceTests
{
    private readonly FakeAuth _auth = new();
    private readonly FakeRemoteStore _remote = new();
    private readonly FakeConnectivity _connectivity = new();
    private readonly LocalStore _local = new(new InMemoryIndexedDb(), new FakeTimeProvider());

    private CollectionService Create() => new(_auth, _remote, _connectivity, _local, NullLogger<CollectionService>.Instance);

    [Fact]
    public async Task Finds_the_collection_shared_with_the_signed_in_user()
    {
        var shared = await _remote.CreateCollectionAsync("Dad's cars", "dad@example.com");
        await _remote.UpdateMembersAsync(shared.Id, ["dad@example.com", "chris@example.com"]);
        _auth.User = new AppUser("uid-2", "Chris@Example.com", "Chris", null);

        var service = Create();
        await service.InitializeAsync();

        Assert.Equal(shared.Id, service.Current?.Id);
    }

    [Fact]
    public async Task No_collection_until_one_is_created()
    {
        var service = Create();
        await service.InitializeAsync();
        Assert.Null(service.Current);

        await service.CreateAsync("  Dad's cars ");

        Assert.Equal("Dad's cars", service.Current!.Name);
        Assert.Equal(["dad@example.com"], service.Current.MemberEmails);
    }

    [Fact]
    public async Task Adding_a_member_normalises_the_email_and_saves_it()
    {
        var service = Create();
        await service.InitializeAsync();
        await service.CreateAsync("Dad's cars");

        await service.AddMemberAsync("  Chris@Example.COM ");
        await service.AddMemberAsync("chris@example.com");

        Assert.Equal(["dad@example.com", "chris@example.com"], _remote.Collections[service.Current!.Id].MemberEmails);
    }

    [Fact]
    public async Task Cannot_remove_yourself()
    {
        var service = Create();
        await service.InitializeAsync();
        await service.CreateAsync("Dad's cars");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveMemberAsync("dad@example.com"));
    }

    [Fact]
    public async Task Remembers_the_collection_for_offline_starts()
    {
        var online = Create();
        await online.InitializeAsync();
        await online.CreateAsync("Dad's cars");

        _connectivity.Set(false);
        var offline = Create();
        await offline.InitializeAsync();

        Assert.Equal(online.Current!.Id, offline.Current?.Id);
    }

    [Fact]
    public async Task Signing_out_clears_the_collection()
    {
        var service = Create();
        await service.InitializeAsync();
        await service.CreateAsync("Dad's cars");

        await _auth.SignOutAsync();

        Assert.Null(service.Current);
    }
}
