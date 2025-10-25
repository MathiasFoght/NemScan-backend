namespace NemScan_API.Models.DTO.Statistics;

public class TopScannedProductDTO
{
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ScanCount { get; set; }
}