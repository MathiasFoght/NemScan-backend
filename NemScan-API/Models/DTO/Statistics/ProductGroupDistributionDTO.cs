namespace NemScan_API.Models.DTO.Statistics;

public class ProductGroupDistributionDTO
{
    public string ProductGroup { get; set; } = string.Empty;
    public int ScanCount { get; set; }
    public double Percentage { get; set; }
}