namespace EcountManager.Models;

public sealed record CopySnippet(
    Guid Id,
    string Title,
    string Template,
    DateTimeOffset CreatedAt);
