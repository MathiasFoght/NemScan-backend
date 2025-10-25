using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NemScan_API.Models.Events;

public enum ReportType
{
    ProductNotFound,
}

public class ProductScanReportLogEvent
{
    [Key]
    public Guid Id { get; set; }
    
    public string ProductNumber { get; set; } = string.Empty;
    
    public string ProductName { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public ReportType ReportType { get; set; } = ReportType.ProductNotFound;

    public string UserRole { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}