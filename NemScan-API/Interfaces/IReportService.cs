using NemScan_API.Models.DTO.Report;

namespace NemScan_API.Interfaces;

public interface IReportService
{
    Task<bool> CreateReportAsync(Guid scanLogId, string productNumber, string reportType, string userRole);
    Task<List<ErrorPatternDTO>> GetErrorPatternsAsync(string language = "da");
    Task<List<FrequentErrorProductDTO>> GetTop3MostFailedProductsAsync();
}