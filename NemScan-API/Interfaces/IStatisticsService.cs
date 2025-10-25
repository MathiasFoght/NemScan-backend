using NemScan_API.Models.DTO.Product;
using NemScan_API.Models.DTO.Statistics;

namespace NemScan_API.Interfaces;

public interface IStatisticsService
{
    Task<ScanPerformanceDTO> GetScanPerformanceAsync(DateTime? from = null, DateTime? to = null);
    Task<List<ProductGroupDistributionDTO>> GetProductGroupDistributionAsync(DateTime? from = null, DateTime? to = null);
    Task<List<ErrorRateTrendDTO>> GetProductsWithIncreasingErrorRateAsync(int days = 7);
    Task<TopScannedProductDTO?> GetMostScannedProductAsync();
    Task<List<LowStockProductDTO>> GetLowStockProductsAsync(double minThreshold = 100);
    Task<WeeklyScanTrendResponse> GetScanActivityAsync(string periodType = "week");

}