namespace ModelCollection.Models;

/// <summary>A cloud collection shared between the people listed in <see cref="MemberEmails"/>.</summary>
public sealed record SharedCollection(string Id, string Name, IReadOnlyList<string> MemberEmails);
