using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;
using NemScan_API.Models.DTO.Statistics;
using NemScan_API.Utils;

namespace NemScan_API.Services.Statistics;

public class StatisticsService : IStatisticsService
{
    private readonly NemScanDbContext _db;
    
    private readonly HttpClient _httpClient;
    
    private readonly IAmeroAuthService _ameroAuthService;

    private static readonly string[] _periods =
    {
        "Morning", "Late Morning", "Afternoon", "Evening"
    };

    private static readonly string[] _daysOfWeek =
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };

    public StatisticsService(NemScanDbContext db, HttpClient httpClient, IAmeroAuthService ameroAuthService)
    {
        _db = db;
        _httpClient = httpClient;
        _ameroAuthService = ameroAuthService;
    }
    
    public async Task<ScanPerformanceDTO> GetScanPerformanceAsync(DateTime? from = null, DateTime? to = null)
    {
        var today = DateTime.UtcNow;

        var startOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
        var endOfCurrentMonth = startOfCurrentMonth.AddMonths(1).AddTicks(-1);

        var startOfPrevMonth = startOfCurrentMonth.AddMonths(-1);
        var endOfPrevMonth = startOfCurrentMonth.AddTicks(-1);

        var startDate = from ?? startOfCurrentMonth;
        var endDate = to ?? endOfCurrentMonth;

        var currentScans = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate)
            .ToListAsync();

        var prevScans = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= startOfPrevMonth && p.Timestamp <= endOfPrevMonth)
            .ToListAsync();

        double currentSuccessRate = currentScans.Any()
            ? (double)currentScans.Count(s => s.Success) / currentScans.Count * 100
            : 0;

        double prevSuccessRate = prevScans.Any()
            ? (double)prevScans.Count(s => s.Success) / prevScans.Count * 100
            : 0;

        var trend = prevSuccessRate == 0 ? 0 : ((currentSuccessRate - prevSuccessRate) / prevSuccessRate) * 100;
        return new ScanPerformanceDTO
        {
            TotalScans = currentScans.Count,
            SuccessfulScans = currentScans.Count(s => s.Success),
            FailedScans = currentScans.Count(s => !s.Success),
            SuccessRate = Math.Round(currentSuccessRate, 1, MidpointRounding.AwayFromZero),
            Trend = Math.Round(trend, 1, MidpointRounding.AwayFromZero)
        };
    }
    
    public async Task<List<ProductGroupDistributionDTO>> GetProductGroupDistributionAsync(DateTime? from = null, DateTime? to = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var grouped = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate)
            .GroupBy(p => p.ProductGroup)
            .Select(g => new ProductGroupDistributionDTO
            {
                ProductGroup = g.Key ?? "Andet",
                ScanCount = g.Count()
            })
            .OrderByDescending(x => x.ScanCount)
            .ToListAsync();

        int total = grouped.Sum(x => x.ScanCount);
        grouped.ForEach(x =>
            x.Percentage = total == 0 ? 0 : Math.Round((double)x.ScanCount / total * 100, 1)
        );

        // Mindre kategorier (bortset fra de 5 største) samles under "Andre"
        if (grouped.Count > 6)
        {
            var top = grouped.Take(5).ToList();
            var otherCount = grouped.Skip(5).Sum(x => x.ScanCount);
            top.Add(new ProductGroupDistributionDTO
            {
                ProductGroup = "Other",
                ScanCount = otherCount,
                Percentage = total == 0 ? 0 : Math.Round((double)otherCount / total * 100, 1)
            });
            grouped = top;
        }

        return grouped;
    }
    
    public async Task<List<ErrorRateTrendDTO>> GetProductsWithIncreasingErrorRateAsync(int days = 7)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);
        var prevStartDate = endDate.AddDays(-days * 2);
        var prevEndDate = endDate.AddDays(-days);

        var scanQuery = _db.ProductScanLogs.AsQueryable();
        var reportQuery = _db.ProductScanReportLogs.AsQueryable();

        var currentScans = await scanQuery
            .Where(s => s.Timestamp >= startDate && s.Timestamp <= endDate)
            .GroupBy(s => new { s.ProductNumber, s.ProductName })
            .Select(g => new
            {
                g.Key.ProductNumber,
                g.Key.ProductName,
                ScanCount = g.Count()
            })
            .ToListAsync();

        var previousScans = await scanQuery
            .Where(s => s.Timestamp >= prevStartDate && s.Timestamp <= prevEndDate)
            .GroupBy(s => new { s.ProductNumber, s.ProductName })
            .Select(g => new
            {
                g.Key.ProductNumber,
                g.Key.ProductName,
                ScanCount = g.Count()
            })
            .ToListAsync();

        var currentReports = await reportQuery
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .GroupBy(r => new { r.ProductNumber, r.ProductName })
            .Select(g => new
            {
                g.Key.ProductNumber,
                ProductName = string.IsNullOrWhiteSpace(g.Key.ProductName) ? "Unknown" : g.Key.ProductName,
                ErrorCount = g.Count()
            })
            .ToListAsync();

        var previousReports = await reportQuery
            .Where(r => r.CreatedAt >= prevStartDate && r.CreatedAt <= prevEndDate)
            .GroupBy(r => new { r.ProductNumber, r.ProductName })
            .Select(g => new
            {
                g.Key.ProductNumber,
                ProductName = string.IsNullOrWhiteSpace(g.Key.ProductName) ? "Unknown" : g.Key.ProductName,
                ErrorCount = g.Count()
            })
            .ToListAsync();

        var results = (from c in currentScans
                       join cr in currentReports on c.ProductNumber equals cr.ProductNumber into crg
                       from cr in crg.DefaultIfEmpty()
                       join p in previousScans on c.ProductNumber equals p.ProductNumber into pg
                       from p in pg.DefaultIfEmpty()
                       join pr in previousReports on c.ProductNumber equals pr.ProductNumber into prg
                       from pr in prg.DefaultIfEmpty()
                       let currentErrorRate = c.ScanCount == 0 ? 0 :
                           ((cr?.ErrorCount ?? 0) / (double)c.ScanCount) * 100
                       let previousErrorRate = (p == null || p.ScanCount == 0) ? 0 :
                           ((pr?.ErrorCount ?? 0) / (double)p.ScanCount) * 100
                       let trendChange = previousErrorRate == 0 ? currentErrorRate :
                           ((currentErrorRate - previousErrorRate) / previousErrorRate) * 100
                       select new ErrorRateTrendDTO
                       {
                           ProductNumber = c.ProductNumber,
                           ProductName = c.ProductName,
                           CurrentErrorRate = Math.Round(currentErrorRate, 2),
                           PreviousErrorRate = Math.Round(previousErrorRate, 2),
                           TrendChange = Math.Round(trendChange, 2)
                       })
            .Where(x => x.TrendChange > 0)
            .OrderByDescending(x => x.TrendChange)
            .Take(5)
            .ToList();

        return results;
    }
    
    public async Task<TopScannedProductDTO?> GetMostScannedProductAsync()
    {
        var start = DateTime.UtcNow.Date;
        var end = DateTime.UtcNow;

        var top = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= start && p.Timestamp <= end)
            .GroupBy(p => new { p.ProductNumber, p.ProductName })
            .Select(g => new TopScannedProductDTO
            {
                ProductNumber = g.Key.ProductNumber,
                ProductName = g.Key.ProductName,
                ScanCount = g.Count()
            })
            .OrderByDescending(x => x.ScanCount)
            .FirstOrDefaultAsync();

        return top;
    }
    
    public async Task<WeeklyScanTrendResponse> GetScanActivityAsync(string periodType = "week")
    {
        var now = DateTime.UtcNow;
        DateTime from, to;

        if (periodType.Equals("month", StringComparison.OrdinalIgnoreCase))
        {
            from = new DateTime(now.Year, now.Month, 1);
            to = now;
        }
        else
        {
            from = StartOfWeek(now, DayOfWeek.Monday);
            to = from.AddDays(7);
        }

        var logs = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= from && p.Timestamp <= to)
            .ToListAsync();

        var copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        if (periodType == "week")
        {
            var grouped = logs
                .GroupBy(p =>
                {
                    var localTime = TimeZoneInfo.ConvertTime(p.Timestamp, copenhagenZone);
                    return new
                    {
                        Day = localTime.DayOfWeek,
                        Period = GetPeriodFromHour(localTime.Hour)
                    };
                })
                .Select(g => new WeeklyScanHeatmapDTO
                {
                    Day = g.Key.Day.ToString(),
                    Period = g.Key.Period,
                    Count = g.Count()
                })
                .ToList();

            var result = (
                from day in _daysOfWeek
                from period in _periods
                join g in grouped on new { Day = day, Period = period }
                    equals new { g.Day, g.Period } into gj
                from item in gj.DefaultIfEmpty()
                select new WeeklyScanHeatmapDTO
                {
                    Day = day,
                    Period = period,
                    Count = item?.Count ?? 0
                }
            )
            .OrderBy(x => Array.IndexOf(_daysOfWeek, x.Day))
            .ThenBy(x => Array.IndexOf(_periods, x.Period))
            .ToList();

            return new WeeklyScanTrendResponse
            {
                PeriodType = "week",
                Heatmap = result
            };
        }

        var dailyCounts = logs
            .GroupBy(p => TimeZoneInfo.ConvertTime(p.Timestamp, copenhagenZone).Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Rolling average med et vindue på 7 dage
        var rollingAverage = new List<WeeklyScanTrendDTO>();

        for (int i = 0; i < dailyCounts.Count; i++)
        {
            var window = dailyCounts
                .Skip(Math.Max(0, i - 6))
                .Take(i >= 6 ? 7 : i + 1) 
                .Select(x => x.Count)
                .ToList();

            double avg = window.Average();
            
            // Vægtet gennemsnit med en vægt på 1 + 0.1 for hver dag i vinduet (De sidste dage har den største vægt), som gør vi tager højde for pludselige udsving
            if (window.Count > 1)
            {
                double weighted = 0;
                double totalWeight = 0;
                for (int j = 0; j < window.Count; j++)
                {
                    double weight = 1 + j * 0.1;
                    weighted += window[j] * weight;
                    totalWeight += weight;
                }
                avg = weighted / totalWeight;
            }

            rollingAverage.Add(new WeeklyScanTrendDTO
            {
                DayOrDate = dailyCounts[i].Date.ToString("dd MMM", new System.Globalization.CultureInfo("da-DK")),
                RollingAverage = Math.Round(avg, 1),
                Count = dailyCounts[i].Count
            });
        }

        return new WeeklyScanTrendResponse
        {
            PeriodType = "month",
            Trend = rollingAverage
        };
    }
    
    public async Task<List<LowStockProductDTO>> GetLowStockProductsAsync(double minThreshold = 100)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

        var requestUrl =
            $"https://api.flexpos.com/api/v1.0/product?offset=0&limit=500" +
            $"&filters[CurrentStockQuantity][$lt]={minThreshold}" +
            $"&fields=CurrentStockQuantity&fields=Number&fields=Name&fields=DisplayProductGroupUid";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<LowStockProductDTO>();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("Items", out var items))
            return new List<LowStockProductDTO>();

        var lowStockProducts = new List<LowStockProductDTO>();

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("CurrentStockQuantity", out var stockProp) ||
                stockProp.ValueKind != JsonValueKind.Number)
                continue;

            var stock = stockProp.GetDecimal();

            if (stock >= (decimal)minThreshold)
                continue;

            // UID for DisplayGroup
            string? groupUid = null;
            if (item.TryGetProperty("DisplayProductGroupUid", out var groupUidProp))
                groupUid = groupUidProp.GetString();

            string productGroupName = "Unknown";

            // Get DisplayGroup name using groupUid
            if (!string.IsNullOrEmpty(groupUid))
            {
                var groupUrl = $"https://api.flexpos.com/api/v2.0/display-group/{groupUid}?fields=Name";
                var groupRequest = new HttpRequestMessage(HttpMethod.Get, groupUrl);
                groupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var groupResponse = await _httpClient.SendAsync(groupRequest);
                if (groupResponse.IsSuccessStatusCode)
                {
                    var groupContent = await groupResponse.Content.ReadAsStringAsync();
                    using var groupDoc = JsonDocument.Parse(groupContent);

                    if (groupDoc.RootElement.TryGetProperty("Name", out var nameProp))
                        productGroupName = nameProp.GetString() ?? "Unknown";
                }
            }

            var dto = new LowStockProductDTO
            {
                ProductNumber = item.GetProperty("Number").GetString() ?? "",
                ProductName = item.GetProperty("Name").GetString() ?? "",
                ProductGroup = productGroupName,
                CurrentStockQuantity = stock
            };

            lowStockProducts.Add(dto);
        }

        return lowStockProducts
            .OrderBy(p => p.CurrentStockQuantity)
            .ToList();
    }
    
    private static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }

    private static string GetPeriodFromHour(int hour) => hour switch
    {
        >= 7 and < 10 => "Morning",
        >= 10 and < 13 => "Late Morning",
        >= 13 and < 18 => "Afternoon",
        _ => "Evening"
    };
    
    
}
