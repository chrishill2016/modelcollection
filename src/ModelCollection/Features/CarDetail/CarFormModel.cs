using ModelCollection.Models;

namespace ModelCollection.Features.CarDetail;

/// <summary>Mutable form model for adding/editing a car.</summary>
public sealed class CarFormModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int? Year { get; set; }
    public string Manufacturer { get; set; } = "";
    public string Scale { get; set; } = "";
    public string Colour { get; set; } = "";
    public CarCondition Condition { get; set; } = CarCondition.Excellent;
    public bool HasBox { get; set; }
    public string Series { get; set; } = "";
    public string CatalogueNumber { get; set; } = "";
    public int? EditionNumber { get; set; }
    public int? EditionSize { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string Location { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Tags { get; set; } = "";

    public static readonly string[] CommonManufacturers =
    [
        "AUTOart", "Bburago", "Burago", "Corgi", "Dinky", "Ertl", "GreenLight", "Hot Wheels", "Johnny Lightning",
        "Kyosho", "Maisto", "Matchbox", "Minichamps", "Norev", "Oxford Diecast", "Solido", "Spark", "Tomica",
    ];

    public static readonly string[] CommonScales = ["1:12", "1:18", "1:24", "1:32", "1:36", "1:43", "1:64", "1:76", "1:87"];

    /// <summary>Validation for the edition number against the edition size; null when fine.</summary>
    public string? EditionError =>
        EditionNumber is { } n && EditionSize is { } size && n > size ? $"Can't be more than the edition size ({size:N0})" : null;

    public static CarFormModel From(ModelCar car) => new()
    {
        Id = car.Id,
        Make = car.Make,
        Model = car.Model,
        Year = car.Year,
        Manufacturer = car.Manufacturer,
        Scale = car.Scale,
        Colour = car.Colour,
        Condition = car.Condition,
        HasBox = car.HasBox,
        Series = car.Series,
        CatalogueNumber = car.CatalogueNumber,
        EditionNumber = car.EditionNumber,
        EditionSize = car.EditionSize,
        PurchaseDate = car.PurchaseDate?.ToDateTime(TimeOnly.MinValue),
        PurchasePrice = car.PurchasePrice,
        EstimatedValue = car.EstimatedValue,
        Location = car.Location,
        Notes = car.Notes,
        Tags = string.Join(", ", car.Tags),
    };

    /// <summary>Applies the form onto <paramref name="original"/> (or a new car), keeping sync metadata.</summary>
    public ModelCar ToCar(ModelCar? original = null) => (original ?? new ModelCar { Id = Id }) with
    {
        Make = Make.Trim(),
        Model = Model.Trim(),
        Year = Year,
        Manufacturer = Manufacturer.Trim(),
        Scale = Scale.Trim(),
        Colour = Colour.Trim(),
        Condition = Condition,
        HasBox = HasBox,
        Series = Series.Trim(),
        CatalogueNumber = CatalogueNumber.Trim(),
        EditionNumber = EditionNumber,
        EditionSize = EditionSize,
        PurchaseDate = PurchaseDate is { } d ? DateOnly.FromDateTime(d) : null,
        PurchasePrice = PurchasePrice,
        EstimatedValue = EstimatedValue,
        Location = Location.Trim(),
        Notes = Notes.Trim(),
        Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
    };
}
