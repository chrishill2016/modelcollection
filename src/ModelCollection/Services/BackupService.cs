using System.Text.Json;
using ModelCollection.Models;

namespace ModelCollection.Services;

public sealed class BackupService(ILocalStore local, BrowserInterop browser, TimeProvider clock) : IBackupService
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public async Task ExportJsonAsync()
    {
        var json = JsonSerializer.Serialize(await CreateBackupAsync(), JsonOptions);
        await browser.InvokeVoidAsync("downloadText", $"model-cars-{clock.GetLocalNow():yyyy-MM-dd}.json", "application/json", json);
    }

    public async Task ExportCsvAsync()
    {
        var cars = (await local.GetCarsAsync()).OrderBy(c => c.Make).ThenBy(c => c.Model);
        await browser.InvokeVoidAsync("downloadText", $"model-cars-{clock.GetLocalNow():yyyy-MM-dd}.csv", "text/csv", CarCsv.Write(cars));
    }

    internal async Task<CollectionBackup> CreateBackupAsync()
    {
        var cars = (await local.GetCarsAsync()).ToList();
        return new CollectionBackup(
            CollectionBackup.CurrentFormatVersion,
            clock.GetUtcNow(),
            cars,
            await local.GetAllPhotosAsync(),
            await local.GetAllThumbnailsAsync());
    }

    public async Task<int> ImportJsonAsync(Stream json)
    {
        var backup = await JsonSerializer.DeserializeAsync<CollectionBackup>(json, JsonOptions)
            ?? throw new InvalidDataException("The file is empty.");
        if (backup.FormatVersion > CollectionBackup.CurrentFormatVersion)
        {
            throw new InvalidDataException("This backup was made by a newer version of the app.");
        }

        var imported = 0;
        foreach (var car in backup.Cars ?? [])
        {
            if (await local.ImportCarAsync(car))
            {
                imported++;
            }
        }

        var existingPhotoIds = (await local.GetAllPhotosAsync()).Select(p => p.Id).ToHashSet();
        var thumbnails = (backup.Thumbnails ?? []).ToDictionary(t => t.Id);
        foreach (var photo in backup.Photos ?? [])
        {
            if (existingPhotoIds.Contains(photo.Id))
            {
                continue;
            }

            await local.AddPhotoAsync(photo, thumbnails.GetValueOrDefault(photo.Id) ?? photo);
        }

        return imported;
    }
}
