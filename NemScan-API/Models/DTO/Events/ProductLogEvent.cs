namespace NemScan_API.Models.DTO.Events;

public class ProductLogEvent
{
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? DisplayProductGroupUid { get; set; }
    public decimal? CurrentSalesPrice { get; set; }
    public decimal? CurrentStockQuantity { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}