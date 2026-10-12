using Microsoft.AspNetCore.Components.Forms;
using ModelCollection.Models;

namespace ModelCollection.Services;

public interface IPhotoService
{
    /// <summary>Compresses an uploaded or camera image and stores it (plus a thumbnail) against a car.</summary>
    Task<CarPhoto> AddPhotoAsync(string carId, IBrowserFile file);

    /// <summary>
    /// The full-size image for a photo: from this device if present, otherwise downloaded from the cloud
    /// and kept for next time. Returns null if it isn't available (e.g. offline).
    /// </summary>
    Task<string?> GetFullImageAsync(CarPhoto thumbnail);
}
