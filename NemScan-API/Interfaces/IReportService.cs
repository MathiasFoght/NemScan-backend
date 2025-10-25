using NemScan_API.Models.DTO.Report;

namespace NemScan_API.Interfaces;

public interface IReportService
{
    Task<bool> CreateReportAsync(string productNumber, string productName, string userRole);
    Task<List<FrequentErrorProductDTO>> GetTop3MostReportedProductsAsync();
    Task<TodaysReportCountDTO> GetTodaysReportCountAsync();
}