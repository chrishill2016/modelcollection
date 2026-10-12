using Microsoft.AspNetCore.Components.Forms;
using ModelCollection.Models;

namespace ModelCollection.Services;

public interface IPhotoService
{
    /// <summary>Compresses an uploaded or camera image and stores it (plus a thumbnail) against a car.</summary>
    Task<CarPhoto> AddPhotoAsync(string carId, IBrowserFile file);
}
