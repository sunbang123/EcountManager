namespace EcountManager.Models;

public sealed record WorkMemo(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt);
