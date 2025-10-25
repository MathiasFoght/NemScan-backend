namespace NemScan_API.Models.DTO.Statistics;

public class ErrorRateTrendDTO
{
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double CurrentErrorRate { get; set; }
    public double PreviousErrorRate { get; set; }
    public double TrendChange { get; set; }
}