namespace ModelCollection.Models;

/// <summary>The JSON backup file format.</summary>
public sealed record CollectionBackup(
    int FormatVersion,
    DateTimeOffset Exported,
    IReadOnlyList<ModelCar> Cars,
    IReadOnlyList<CarPhoto> Photos,
    IReadOnlyList<CarPhoto> Thumbnails)
{
    public const int CurrentFormatVersion = 1;
}
