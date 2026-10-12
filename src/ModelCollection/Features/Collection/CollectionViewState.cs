namespace ModelCollection.Features.Collection;

/// <summary>Remembers the collection filters while moving between pages.</summary>
public sealed class CollectionViewState
{
    public CollectionQuery Query { get; set; } = new();
    public bool ListView { get; set; }
}
