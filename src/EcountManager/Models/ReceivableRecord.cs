namespace EcountManager.Models;

public sealed record ReceivableRecord(
    DateTime Date,
    string Type,
    string ReferenceNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance,
    string EnteredBy);

