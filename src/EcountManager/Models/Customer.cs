namespace EcountManager.Models;

public sealed record Customer(
    string Code,
    string Name,
    string Representative,
    string Phone,
    string Group,
    string SalesManager,
    string Address,
    decimal CreditLimit);

