using Microsoft.EntityFrameworkCore;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Statistics;
using NemScan_API.Utils;

namespace NemScan_API.Services.Statistics;

public class StatisticsService : IStatisticsService
{
    private readonly NemScanDbContext _db;

    private static readonly string[] _periods =
    {
        "Morning", "Late Morning", "Afternoon", "Evening"
    };

    private static readonly string[] _daysOfWeek =
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };

    public StatisticsService(NemScanDbContext db)
    {
        _db = db;
    }

    public async Task<List<WeeklyScanHeatmapDTO>> GetWeeklyScanHeatmapAsync(DateTime? from = null, DateTime? to = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);
        var endDate = to ?? DateTime.UtcNow;

        var logs = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate)
            .ToListAsync();

        var copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        var grouped = logs
            .GroupBy(p =>
            {
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(p.Timestamp, DateTimeKind.Utc),
                    copenhagenZone
                );

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

        return result;
    }
    
    public async Task<ScanPerformanceDTO> GetScanPerformanceAsync(DateTime? from = null, DateTime? to = null)
    {
        var endDate = to ?? DateTime.UtcNow;
        var startDate = from ?? endDate.AddDays(-7);

        // Sidste uge
        var prevEnd = startDate;
        var prevStart = prevEnd.AddDays(-7);

        var currentScans = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate)
            .ToListAsync();

        var prevScans = await _db.ProductScanLogs
            .Where(p => p.Timestamp >= prevStart && p.Timestamp <= prevEnd)
            .ToListAsync();

        double currentSuccessRate = 0;
        if (currentScans.Any())
            currentSuccessRate = (double)currentScans.Count(s => s.Success) / currentScans.Count * 100;

        double prevSuccessRate = 0;
        if (prevScans.Any())
            prevSuccessRate = (double)prevScans.Count(s => s.Success) / prevScans.Count * 100;

        var trend = currentSuccessRate - prevSuccessRate;

        return new ScanPerformanceDTO
        {
            TotalScans = currentScans.Count,
            SuccessfulScans = currentScans.Count(s => s.Success),
            FailedScans = currentScans.Count(s => !s.Success),
            SuccessRate = Math.Round(currentSuccessRate, 1, MidpointRounding.AwayFromZero),
            Trend = Math.Round(trend, 1, MidpointRounding.AwayFromZero)
        };
    }

    private static string GetPeriodFromHour(int hour) => hour switch
    {
        >= 7 and < 10 => "Morning",
        >= 10 and < 13 => "Late Morning",
        >= 13 and < 18 => "Afternoon",
        _ => "Evening"
    };
}
