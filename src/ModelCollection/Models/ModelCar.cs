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

    // Series and limited editions
    /// <summary>The manufacturer's range or series, e.g. "Corgi Classics" or "Car Culture".</summary>
    public string Series { get; init; } = "";
    public string CatalogueNumber { get; init; } = "";
    /// <summary>This model's number within a limited run, e.g. 1234 in "No. 1,234 of 5,000".</summary>
    public int? EditionNumber { get; init; }
    /// <summary>How many were made in a limited run, e.g. 5000.</summary>
    public int? EditionSize { get; init; }

    // Purchase and value
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? EstimatedValue { get; init; }

    public string Location { get; init; } = "";
    public string Notes { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Photo ids in display order; the first is the cover. Photos not listed follow, oldest first.</summary>
    public IReadOnlyList<string> PhotoOrder { get; init; } = [];

    // Sync metadata
    public DateTimeOffset LastModified { get; init; }
    public bool IsDeleted { get; init; }

    // Records compare lists by reference; compare tags by value instead.
    public bool Equals(ModelCar? other) =>
        other is not null
        && Id == other.Id && Make == other.Make && Model == other.Model && Year == other.Year
        && Manufacturer == other.Manufacturer && Scale == other.Scale && Colour == other.Colour
        && Condition == other.Condition && HasBox == other.HasBox && PurchaseDate == other.PurchaseDate
        && Series == other.Series && CatalogueNumber == other.CatalogueNumber
        && EditionNumber == other.EditionNumber && EditionSize == other.EditionSize
        && PurchasePrice == other.PurchasePrice && EstimatedValue == other.EstimatedValue
        && Location == other.Location && Notes == other.Notes && Tags.SequenceEqual(other.Tags)
        && PhotoOrder.SequenceEqual(other.PhotoOrder)
        && LastModified == other.LastModified && IsDeleted == other.IsDeleted;

    public override int GetHashCode() => HashCode.Combine(Id, LastModified);

    public bool IsLimitedEdition => EditionSize is > 0;

    /// <summary>"No. 1,234 of 5,000", "Limited edition of 5,000" or "No. 1,234"; null if neither is known.</summary>
    public string? EditionLabel => (EditionNumber, EditionSize) switch
    {
        ({ } n, { } size) => $"No. {n:N0} of {size:N0}",
        (null, { } size) => $"Limited edition of {size:N0}",
        ({ } n, null) => $"No. {n:N0}",
        _ => null,
    };

    /// <summary>True for cars added quickly that still lack the model's brand and scale.</summary>
    public bool NeedsDetails => string.IsNullOrWhiteSpace(Manufacturer) && string.IsNullOrWhiteSpace(Scale);

    public string DisplayName => string.Join(' ', new[] { Year?.ToString(), Make, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
