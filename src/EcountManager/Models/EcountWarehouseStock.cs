namespace EcountManager.Models;

public sealed record EcountWarehouseStock(
    string WarehouseCode,
    string WarehouseName,
    string ProductCode,
    string ProductName,
    string Specification,
    decimal Quantity);

