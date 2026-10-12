namespace ModelCollection.Models;

/// <summary>Firebase web config. These values are public identifiers, not secrets; Firestore security rules protect the data.</summary>
public sealed class FirebaseOptions
{
    public string ApiKey { get; set; } = "";
    public string AuthDomain { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string AppId { get; set; } = "";
    public string StorageBucket { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId);

    /// <summary>Photo sync needs a Cloud Storage bucket (Firebase Blaze plan).</summary>
    public bool HasStorage => IsConfigured && !string.IsNullOrWhiteSpace(StorageBucket);
}
