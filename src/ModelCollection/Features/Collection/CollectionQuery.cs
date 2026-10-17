using ModelCollection.Models;

namespace ModelCollection.Features.Collection;

public enum CarSort
{
    RecentlyAdded,
    MakeModel,
    Year,
    Manufacturer,
    Value,
}

/// <summary>Search, filter and sort options for the collection view.</summary>
public sealed record CollectionQuery
{
    public string Search { get; init; } = "";
    public string? Manufacturer { get; init; }
    public string? Scale { get; init; }
    public string? Series { get; init; }
    public bool LimitedOnly { get; init; }
    public string? Make { get; init; }
    public CarCondition? Condition { get; init; }
    public bool? Boxed { get; init; }

    /// <summary>Only cars added with Quick add that still need their details filling in.</summary>
    public bool NeedsDetailsOnly { get; init; }
    public CarSort Sort { get; init; } = CarSort.MakeModel;

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) || Manufacturer is not null || Scale is not null || Series is not null || LimitedOnly
        || Make is not null || Condition is not null || Boxed is not null || NeedsDetailsOnly;

    public IEnumerable<ModelCar> Apply(IEnumerable<ModelCar> cars)
    {
        var terms = Search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = cars.Where(c =>
            terms.All(t => Matches(c, t))
            && (Manufacturer is null || Same(c.Manufacturer, Manufacturer))
            && (Scale is null || Same(c.Scale, Scale))
            && (Series is null || Same(c.Series, Series))
            && (!LimitedOnly || c.IsLimitedEdition)
            && (Make is null || Same(c.Make, Make))
            && (Condition is null || c.Condition == Condition)
            && (Boxed is null || c.HasBox == Boxed)
            && (!NeedsDetailsOnly || c.NeedsDetails));

        return Sort switch
        {
            CarSort.RecentlyAdded => result.OrderByDescending(c => c.PurchaseDate).ThenByDescending(c => c.LastModified),
            CarSort.Year => result.OrderBy(c => c.Year ?? int.MaxValue).ThenBy(c => c.Make).ThenBy(c => c.Model),
            CarSort.Manufacturer => result.OrderBy(c => c.Manufacturer).ThenBy(c => c.Make).ThenBy(c => c.Model),
            CarSort.Value => result.OrderByDescending(c => c.EstimatedValue ?? c.PurchasePrice ?? 0),
            _ => result.OrderBy(c => c.Make).ThenBy(c => c.Model).ThenBy(c => c.Year),
        };
    }

    private static bool Matches(ModelCar car, string term) =>
        Contains(car.Make, term) || Contains(car.Model, term) || Contains(car.Manufacturer, term)
        || Contains(car.Colour, term) || Contains(car.Scale, term) || Contains(car.Location, term)
        || Contains(car.Notes, term) || Contains(car.Series, term) || Contains(car.CatalogueNumber, term)
        || car.Year?.ToString() == term
        || car.Tags.Any(tag => Contains(tag, term));

    private static bool Contains(string value, string term) => value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
