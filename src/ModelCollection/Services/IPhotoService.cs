using Microsoft.AspNetCore.Components.Forms;
using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>A compressed photo (and thumbnail) not yet attached to a car.</summary>
public sealed record PreparedPhoto(string FullDataUrl, string ThumbnailDataUrl);

public interface IPhotoService
{
    /// <summary>Compresses an uploaded or camera image and stores it (plus a thumbnail) against a car.</summary>
    Task<CarPhoto> AddPhotoAsync(string carId, IBrowserFile file);

    /// <summary>Compresses an image without saving it, e.g. while the car is still being created.</summary>
    Task<PreparedPhoto> PrepareAsync(IBrowserFile file);

    /// <summary>Stores a prepared photo against a car.</summary>
    Task<CarPhoto> SavePreparedAsync(string carId, PreparedPhoto photo);

    /// <summary>
    /// The full-size image for a photo: from this device if present, otherwise downloaded from the cloud
    /// and kept for next time. Returns null if it isn't available (e.g. offline).
    /// </summary>
    Task<string?> GetFullImageAsync(CarPhoto thumbnail);
}
