namespace NemScan_API.Models.DTO.Statistics;

public class WeeklyScanHeatmapDTO
{
    public string Day { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}