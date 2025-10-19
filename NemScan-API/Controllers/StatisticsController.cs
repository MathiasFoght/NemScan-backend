using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("scans/weekly-heatmap")]
    public async Task<IActionResult> GetWeeklyScanHeatmap([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var data = await _statisticsService.GetWeeklyScanHeatmapAsync(from, to);

        if (data.Count == 0)
            return NotFound("Ingen scanningsdata fundet i det valgte interval");

        return Ok(data);
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("scans/performance")]
    public async Task<IActionResult> GetScanPerformance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var performance = await _statisticsService.GetScanPerformanceAsync(from, to);

        if (performance.TotalScans == 0)
            return NotFound("Ingen scanningsdata fundet i det valgte interval");

        return Ok(performance);
    }
}