namespace EcountManager.Models;

public sealed record InventoryItem(
    string ItemCode,
    string ItemName,
    string Specification,
    string Unit,
    decimal CurrentStock,
    decimal StandardPrice,
    decimal? LastCustomerPrice,
    decimal? LastOrderQuantity,
    DateTime? LastSaleDate,
    bool TaxIncluded)
{
    public string StockStatus => CurrentStock switch
    {
        <= 0 => "품절",
        < 10 => "재고 부족",
        _ => "주문 가능"
    };

    public string TaxLabel => TaxIncluded ? "부가세 포함" : "부가세 별도";
}

