namespace ModelCollection.Services;

public interface IBackupService
{
    /// <summary>Downloads everything (cars and photos) as a JSON file.</summary>
    Task ExportJsonAsync();

    /// <summary>Downloads the cars as a CSV spreadsheet.</summary>
    Task ExportCsvAsync();

    /// <summary>Merges a JSON backup into the collection (newer copies win). Returns the number of cars imported.</summary>
    Task<int> ImportJsonAsync(Stream json);
}
