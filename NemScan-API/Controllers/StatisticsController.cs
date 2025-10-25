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
    [HttpGet("scans/performance")]
    public async Task<IActionResult> GetScanPerformance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var performance = await _statisticsService.GetScanPerformanceAsync(from, to);

        if (performance.TotalScans == 0)
            return NotFound("No scanning data found in the selected interval");
        
        return Ok(performance);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("scans/product-group-distribution")]
    public async Task<IActionResult> GetProductGroupDistribution([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var data = await _statisticsService.GetProductGroupDistributionAsync(from, to);

        if (data.Count == 0)
            return NotFound("No scanning data found in the selected interval");

        return Ok(data);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("errors/increasing-error-rate")]
    public async Task<IActionResult> GetIncreasingErrorRate([FromQuery] int days = 7)
    {
        var data = await _statisticsService.GetProductsWithIncreasingErrorRateAsync(days);

        if (data.Count == 0)
            return NotFound("No products with an increasing error rate found in the selected period");

        return Ok(data);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("scans/top-product-today")]
    public async Task<IActionResult> GetTopScannedProduct()
    {
        var product = await _statisticsService.GetMostScannedProductAsync();
        if (product == null)
            return Ok(new { productNumber = (string?)null, productName = "No scans today", scanCount = 0 });

        return Ok(product);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("products/low-stock")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] double minThreshold = 100)
    {
        var products = await _statisticsService.GetLowStockProductsAsync(minThreshold);

        if (products == null || products.Count == 0)
            return NotFound("No products with low stock found");

        return Ok(products);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("scans/activity")]
    public async Task<IActionResult> GetScanActivity([FromQuery] string periodType = "week")
    {
        var result = await _statisticsService.GetScanActivityAsync(periodType);
        return Ok(result);
    }
}