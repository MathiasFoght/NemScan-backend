using Microsoft.EntityFrameworkCore;
using NemScan_API.Interfaces;
using NemScan_API.Models.Events;
using NemScan_API.Models.DTO.Report;
using NemScan_API.Utils;

namespace NemScan_API.Services.Report;

public class ReportService : IReportService
{
    private readonly NemScanDbContext _db;

    public ReportService(NemScanDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CreateReportAsync(string productNumber, string productName, string userRole)
    {
        var report = new ProductScanReportLogEvent
        {
            Id = Guid.NewGuid(),
            ProductNumber = productNumber,
            ProductName = productName,
            ReportType = ReportType.ProductNotFound,
            UserRole = userRole,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _db.ProductScanReportLogs.AddAsync(report);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<FrequentErrorProductDTO>> GetTop3MostReportedProductsAsync()
    {
        var grouped = await _db.ProductScanReportLogs
            .GroupBy(r => new
            {
                r.ProductNumber,
                r.ProductName
            })
            .Select(g => new FrequentErrorProductDTO
            {
                ProductNumber = g.Key.ProductNumber,
                ProductName = string.IsNullOrWhiteSpace(g.Key.ProductName) ? "Unknown product" : g.Key.ProductName,
                ErrorCount = g.Count()
            })
            .OrderByDescending(x => x.ErrorCount)
            .Take(3)
            .ToListAsync();

        int total = await _db.ProductScanReportLogs.CountAsync();
        grouped.ForEach(x =>
            x.Percentage = total == 0 ? 0 : Math.Round((double)x.ErrorCount / total * 100, 1)
        );

        return grouped;
    }

    
    public async Task<TodaysReportCountDTO> GetTodaysReportCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var count = await _db.ProductScanReportLogs
            .CountAsync(r => r.CreatedAt >= today && r.CreatedAt < tomorrow);

        return new TodaysReportCountDTO
        {
            TotalReportsToday = count
        };
    }
}
