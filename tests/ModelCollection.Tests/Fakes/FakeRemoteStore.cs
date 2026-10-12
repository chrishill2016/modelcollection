using ModelCollection.Models;
using ModelCollection.Services;

namespace ModelCollection.Tests.Fakes;

/// <summary>In-memory Firestore stand-in with a server clock for pull cursors.</summary>
public sealed class FakeRemoteStore : IRemoteStore
{
    private long _serverClock = 1000;

    public Dictionary<string, SharedCollection> Collections { get; } = [];
    public Dictionary<string, (ModelCar Car, long ServerUpdatedAt)> Cars { get; } = [];
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
