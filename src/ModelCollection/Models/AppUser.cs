namespace ModelCollection.Models;

public sealed record AppUser(string Uid, string Email, string? DisplayName, string? PhotoUrl);
