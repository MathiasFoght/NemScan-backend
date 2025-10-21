using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NemScan_API.Models.Events;

public enum ReportType
{
    ProductNotFound,
    CampaignNotFound,
    MissingInformation
}

public class ProductScanReportLogEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProductScanLogId { get; set; }

    [ForeignKey(nameof(ProductScanLogId))]
    public ProductScanLogEvent? ProductScanLog { get; set; }

    public string ProductNumber { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public ReportType ReportType { get; set; }

    public string UserRole { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}