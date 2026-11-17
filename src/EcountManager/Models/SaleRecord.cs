namespace EcountManager.Models;

public sealed record SaleRecord(
    string SlipNumber,
    DateTime SaleDate,
    string ItemCode,
    string ItemName,
    string Specification,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal SupplyAmount,
    decimal TaxAmount,
    string EnteredBy)
{
    public decimal TotalAmount => SupplyAmount + TaxAmount;
}

