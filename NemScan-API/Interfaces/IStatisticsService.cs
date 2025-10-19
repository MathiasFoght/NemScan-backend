using NemScan_API.Models.DTO.Statistics;

namespace NemScan_API.Interfaces;

public interface IStatisticsService
{
    Task<List<WeeklyScanHeatmapDTO>> GetWeeklyScanHeatmapAsync(DateTime? from = null, DateTime? to = null);
    Task<ScanPerformanceDTO> GetScanPerformanceAsync(DateTime? from = null, DateTime? to = null);
}