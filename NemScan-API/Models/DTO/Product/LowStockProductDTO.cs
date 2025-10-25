namespace NemScan_API.Models.DTO.Product;

public class LowStockProductDTO
{
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductGroup { get; set; } = string.Empty;
    public decimal CurrentStockQuantity { get; set; }
}