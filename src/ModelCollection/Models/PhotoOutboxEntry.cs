namespace ModelCollection.Models;

public enum PhotoOp
{
    Upload,
    Delete,
}

/// <summary>A photo upload or deletion waiting to be pushed to cloud storage.</summary>
public sealed record PhotoOutboxEntry(string PhotoId, string CarId, PhotoOp Op, DateTimeOffset Queued);
