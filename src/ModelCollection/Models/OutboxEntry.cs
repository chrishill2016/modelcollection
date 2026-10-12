namespace ModelCollection.Models;

/// <summary>A car that has local changes waiting to be pushed to the cloud.</summary>
public sealed record OutboxEntry(string CarId, DateTimeOffset Queued);
