namespace NemScan_API.Models.DTO.Statistics;

public class ScanPerformanceDTO
{
    public int TotalScans { get; set; }
    public int SuccessfulScans { get; set; }
    public int FailedScans { get; set; }
    public double SuccessRate { get; set; }
    public double Trend { get; set; }

}