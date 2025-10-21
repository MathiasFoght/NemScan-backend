namespace NemScan_API.Models.DTO.Report;

public class ErrorPatternDTO
{
    public string ReportType { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Label { get; set; } = string.Empty;
    public double Percentage { get; set; }
}