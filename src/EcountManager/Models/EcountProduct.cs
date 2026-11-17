namespace EcountManager.Models;

public sealed class EcountProduct
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Specification { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public decimal PurchasePrice { get; init; }
    public decimal SalesPrice { get; init; }
    public decimal CurrentStock { get; set; }
    public decimal TaxRate { get; init; }

    public string StockStatus => CurrentStock switch
    {
        <= 0 => "품절",
        < 10 => "재고 부족",
        _ => "주문 가능"
    };

    public string ProductTypeName => ProductType switch
    {
        "0" => "원재료",
        "1" => "제품",
        "2" => "반제품",
        "3" => "상품",
        "4" => "부재료",
        "7" => "무형상품",
        _ => ProductType
    };
}

