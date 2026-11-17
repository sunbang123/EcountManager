namespace EcountManager.Models;

public sealed record CustomerDashboard(
    Customer Customer,
    IReadOnlyList<SaleRecord> Sales,
    IReadOnlyList<InventoryItem> Inventory,
    IReadOnlyList<ReceivableRecord> Receivables)
{
    public decimal OutstandingBalance => Receivables.Count == 0 ? 0 : Receivables[^1].Balance;
    public DateTime? LastSaleDate => Sales.Count == 0 ? null : Sales.Max(x => x.SaleDate);
    public decimal RecentSalesTotal => Sales.Sum(x => x.TotalAmount);
}

