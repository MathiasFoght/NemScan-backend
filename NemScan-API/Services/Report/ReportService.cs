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

    // Create a report
    public async Task<bool> CreateReportAsync(Guid scanLogId, string productNumber, string reportType, string userRole)
    {
        if (!Enum.TryParse<ReportType>(reportType, true, out var parsedType))
            throw new ArgumentException("Invalid report type");

        var scanLog = await _db.ProductScanLogs.FindAsync(scanLogId);
        if (scanLog == null)
            return false;

        var report = new ProductScanReportLogEvent
        {
            Id = Guid.NewGuid(),
            ProductScanLogId = scanLogId,
            ProductNumber = productNumber,
            ReportType = parsedType,
            UserRole = userRole,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _db.ProductScanReportLogs.AddAsync(report);
        await _db.SaveChangesAsync();

        return true;
    }

    // Error patterns
    public async Task<List<ErrorPatternDTO>> GetErrorPatternsAsync(string language = "da")
    {
        var grouped = await _db.ProductScanReportLogs
            .GroupBy(r => r.ReportType)
            .Select(g => new ErrorPatternDTO
            {
                ReportType = g.Key.ToString(),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        int total = grouped.Sum(x => x.Count);
        grouped.ForEach(x =>
        {
            x.Percentage = total == 0 ? 0 : Math.Round((double)x.Count / total * 100, 1);
            x.Label = language switch
            {
                "da" => x.ReportType switch
                {
                    "ProductNotFound" => "Produkt kunne ikke findes",
                    "CampaignNotFound" => "Kampagne ikke fundet",
                    "MissingInformation" => "Manglende information",
                    _ => x.ReportType
                },
                _ => x.ReportType switch
                {
                    "ProductNotFound" => "Product not found",
                    "CampaignNotFound" => "Campaign not found",
                    "MissingInformation" => "Missing information",
                    _ => x.ReportType
                }
            };
        });

        return grouped;
    }

    // Top 3 most problematic products
    public async Task<List<FrequentErrorProductDTO>> GetTop3MostFailedProductsAsync()
    {
        var grouped = await _db.ProductScanReportLogs
            .GroupBy(r => new
            {
                r.ProductNumber,
                ProductName = r.ProductScanLog != null ? r.ProductScanLog.ProductName : "Ukendt produkt"
            })            
            .Select(g => new FrequentErrorProductDTO
            {
                ProductNumber = g.Key.ProductNumber,
                ProductName = g.Key.ProductName,
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
}
