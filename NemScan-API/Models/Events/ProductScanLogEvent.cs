using System.ComponentModel.DataAnnotations;

namespace NemScan_API.Models.Events;

public class ProductScanLogEvent
{
    [Key]
    public Guid Id { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? CurrentSalesPrice { get; set; }
    public decimal? CurrentStockQuantity { get; set; }
    public string? ProductGroup { get; set; }
    public bool Success { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}