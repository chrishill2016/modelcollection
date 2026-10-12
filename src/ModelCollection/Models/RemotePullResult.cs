namespace ModelCollection.Models;

/// <summary>Cars changed in the cloud since a cursor, plus the cursor to use next time.</summary>
public sealed record RemotePullResult(IReadOnlyList<ModelCar> Cars, long Cursor);
