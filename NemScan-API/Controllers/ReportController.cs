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
        if (string.IsNullOrWhiteSpace(request.ProductNumber))
            return BadRequest("Product number is required");

        var success = await _reportService.CreateReportAsync(
            request.ProductNumber,
            request.ProductName,
            request.UserRole
        );

        if (!success)
            return StatusCode(500, "Could not create report");
        return Ok("Report created");
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("top-3-most-failed-products")]
    public async Task<IActionResult> GetTop3MostReportedProducts()
    {
        var data = await _reportService.GetTop3MostReportedProductsAsync();
        
        if (data == null || data.Count == 0)
            return NotFound("No reports found");
        
        return Ok(data);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("reports/count-today")]
    public async Task<IActionResult> GetTodaysReportCount()
    {
        var count = await _reportService.GetTodaysReportCountAsync();
        
        return Ok(new { totalReportsToday = count });
    }

}

public class ReportRequest
{
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
}