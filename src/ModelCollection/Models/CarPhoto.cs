namespace ModelCollection.Models;

/// <summary>
/// A compressed photo of a car, stored as a data URL. Full-size photos and thumbnails are kept in
/// separate stores with the same id so the collection grid only loads thumbnails. Photos synced from
/// another device arrive as a thumbnail first; the full-size image is fetched when the car is viewed.
/// </summary>
public sealed record CarPhoto(string Id, string CarId, string DataUrl, DateTimeOffset Created);
