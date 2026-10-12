namespace ModelCollection.Models;

/// <summary>A single model car in the collection.</summary>
public sealed record ModelCar
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    // The real car
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int? Year { get; init; }

    // The model itself
    public string Manufacturer { get; init; } = "";
    public string Scale { get; init; } = "";
    public string Colour { get; init; } = "";
    public CarCondition Condition { get; init; } = CarCondition.Excellent;
    public bool HasBox { get; init; }

    // Purchase and value
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? EstimatedValue { get; init; }

    public string Location { get; init; } = "";
    public string Notes { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];

    // Sync metadata
    public DateTimeOffset LastModified { get; init; }
    public bool IsDeleted { get; init; }

    // Records compare lists by reference; compare tags by value instead.
    public bool Equals(ModelCar? other) =>
        other is not null
        && Id == other.Id && Make == other.Make && Model == other.Model && Year == other.Year
        && Manufacturer == other.Manufacturer && Scale == other.Scale && Colour == other.Colour
        && Condition == other.Condition && HasBox == other.HasBox && PurchaseDate == other.PurchaseDate
        && PurchasePrice == other.PurchasePrice && EstimatedValue == other.EstimatedValue
        && Location == other.Location && Notes == other.Notes && Tags.SequenceEqual(other.Tags)
        && LastModified == other.LastModified && IsDeleted == other.IsDeleted;

    public override int GetHashCode() => HashCode.Combine(Id, LastModified);

    public string DisplayName => string.Join(' ', new[] { Year?.ToString(), Make, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
