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
        "Morning", "Late Morning", "Afternoon", "Evening", "Closed Hours"
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

        var grouped = logs
            .GroupBy(p => new
            {
                Day = p.Timestamp.DayOfWeek,
                Period = GetPeriodFromHour(p.Timestamp.Hour)
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

    private static string GetPeriodFromHour(int hour) => hour switch
    {
        >= 7 and < 10 => "Morning",
        >= 10 and < 12 => "Late Morning",
        >= 12 and < 18 => "Afternoon",
        >= 18 and < 22 => "Evening",
        _ => "Closed Hours"
    };
}
