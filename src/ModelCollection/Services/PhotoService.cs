using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class PhotoService(
    BrowserInterop browser,
    ILocalStore local,
    TimeProvider clock,
    IRemoteStore remote,
    ICollectionService collections,
    IConnectivityService connectivity,
    ILogger<PhotoService> logger) : IPhotoService
{
    internal const long MaxUploadBytes = 30 * 1024 * 1024;
    private const int FullSize = 1600;
    private const int ThumbnailSize = 400;

    public async Task<CarPhoto> AddPhotoAsync(string carId, IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(MaxUploadBytes);
        using var streamRef = new DotNetStreamReference(stream);
        var resized = await browser.InvokeAsync<ResizedImage>("resizeImage", streamRef, FullSize, ThumbnailSize);

        var id = Guid.NewGuid().ToString("N");
        var created = clock.GetUtcNow();
        var photo = new CarPhoto(id, carId, resized.Full, created);
        await local.AddPhotoAsync(photo, photo with { DataUrl = resized.Thumbnail });
        return photo;
    }

    public async Task<string?> GetFullImageAsync(CarPhoto thumbnail)
    {
        if (await local.GetPhotoAsync(thumbnail.Id) is { } full)
        {
            return full.DataUrl;
        }

        if (!remote.SupportsPhotos || !connectivity.IsOnline || collections.Current is not { } collection)
        {
            return null;
        }

        try
        {
            var dataUrl = await remote.DownloadPhotoAsync(collection.Id, thumbnail.Id, thumbnail: false);
            await local.SaveDownloadedPhotoAsync(thumbnail with { DataUrl = dataUrl });
            return dataUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not download photo {PhotoId}", thumbnail.Id);
            return null;
        }
    }

    private sealed record ResizedImage(string Full, string Thumbnail);
}
