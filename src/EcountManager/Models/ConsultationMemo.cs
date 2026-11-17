namespace EcountManager.Models;

public sealed record ConsultationMemo(
    Guid Id,
    string BusinessName,
    string PhoneNumber,
    string Inquiry,
    DateTimeOffset CreatedAt);

