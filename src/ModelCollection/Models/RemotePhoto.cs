namespace ModelCollection.Models;

/// <summary>Cloud metadata for a photo; the image itself lives in cloud storage.</summary>
public sealed record RemotePhoto(string Id, string CarId, DateTimeOffset Created, bool IsDeleted);

public sealed record RemotePhotoPullResult(IReadOnlyList<RemotePhoto> Photos, long Cursor);
