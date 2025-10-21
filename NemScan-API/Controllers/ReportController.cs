using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [Authorize(Policy = "EmployeeOrCustomer")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateReport([FromBody] ReportRequest request)
    {
        var success = await _reportService.CreateReportAsync(
            request.ProductScanLogId,
            request.ProductNumber,
            request.ReportType,
            request.UserRole
        );

        if (!success)
            return NotFound("Scan log id ikke fundet");

        return Ok("Rapport oprettet");
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("error-patterns")]
    public async Task<IActionResult> GetErrorPatterns([FromQuery] string language = "da")
    {
        var data = await _reportService.GetErrorPatternsAsync(language);
        return Ok(data);
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("top-3-most-failed-products")]
    public async Task<IActionResult> GetTop3MostFailedProducts()
    {
        var data = await _reportService.GetTop3MostFailedProductsAsync();
        return Ok(data);
    }
}

public class ReportRequest
{
    public Guid ProductScanLogId { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
}