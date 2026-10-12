using ModelCollection.Models;

namespace ModelCollection.Tests;

public static class TestData
{
    public static ModelCar Car(string make = "Ferrari", string model = "250 GTO", string? id = null, DateTimeOffset? modified = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString("N"),
        Make = make,
        Model = model,
        Year = 1962,
        Manufacturer = "Corgi",
        Scale = "1:43",
        Colour = "Red",
        LastModified = modified ?? DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
    };
}
