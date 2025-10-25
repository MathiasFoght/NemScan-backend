namespace NemScan_API.Models.DTO.Statistics;

public class WeeklyScanHeatmapDTO
{
    public string Day { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WeeklyScanTrendDTO
{
    public string DayOrDate { get; set; } = string.Empty;
    public double RollingAverage { get; set; }
    public int Count { get; set; }
}

public class WeeklyScanTrendResponse
{
    public string PeriodType { get; set; } = "week";
    public List<WeeklyScanHeatmapDTO> Heatmap { get; set; } = new();
    public List<WeeklyScanTrendDTO> Trend { get; set; } = new();
}