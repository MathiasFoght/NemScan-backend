using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;
using NemScan_API.Models.Events;
using NemScan_API.Utils;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductCustomerService _customerService;
    private readonly IProductEmployeeService _employeeService;
    private readonly IProductImageService _productImageService;
    private readonly ILogEventPublisher _logEventPublisher;
    private readonly IProductCampaignService _productCampaignService;
    private readonly NemScanDbContext _db;


    public ProductController(
        IProductCustomerService customerService,
        IProductEmployeeService employeeService,
        IProductImageService productImageService,
        ILogEventPublisher logEventPublisher,
        IProductCampaignService productCampaignService,
        NemScanDbContext db)
    {
        _customerService = customerService;
        _employeeService = employeeService;
        _productImageService = productImageService;
        _logEventPublisher = logEventPublisher;
        _productCampaignService = productCampaignService;
        _db = db;
    }
    
    [Authorize(Policy = "CustomerOnly")]   
    [HttpGet("customer/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForCustomer(string barcode)
    {
        var timestamp = DateTime.UtcNow;
        var product = await _customerService.GetProductByBarcodeAsync(barcode);

        ProductScanLogEvent scanLog;

        if (product == null)
        {
            scanLog = new ProductScanLogEvent
            {
                Id = Guid.NewGuid(),
                ProductNumber = barcode,
                Success = false,
                UserRole = "customer",
                Timestamp = timestamp
            };
        }
        else
        {
            scanLog = new ProductScanLogEvent
            {
                Id = Guid.NewGuid(),
                ProductNumber = product.ProductNumber,
                ProductName = product.ProductName,
                CurrentSalesPrice = product.CurrentSalesPrice,
                ProductGroup = product.ProductGroup,
                Success = true,
                UserRole = "customer",
                Timestamp = timestamp
            };
        }

        _db.ProductScanLogs.Add(scanLog);
        await _db.SaveChangesAsync();

        await _logEventPublisher.PublishAsync(scanLog, scanLog.Success
            ? "product.scan.success"
            : "product.scan.failed");

        if (!scanLog.Success)
            return NotFound(new
            {
                message = "Produkt ikke fundet",
                scanLogId = scanLog.Id
            });

        return Ok(new
        {
            scanLogId = scanLog.Id,
            product
        });
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("employee/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForEmployee(string barcode)
    {
        var product = await _employeeService.GetProductByBarcodeAsync(barcode);
        var timestamp = DateTime.UtcNow;

        ProductScanLogEvent scanLog;

        if (product == null)
        {
            scanLog = new ProductScanLogEvent
            {
                Id = Guid.NewGuid(),
                ProductNumber = barcode,
                Success = false,
                UserRole = "employee",
                Timestamp = timestamp
            };
        }
        else
        {
            scanLog = new ProductScanLogEvent
            {
                Id = Guid.NewGuid(),
                ProductNumber = product.ProductNumber,
                ProductName = product.ProductName,
                CurrentSalesPrice = product.CurrentSalesPrice,
                CurrentStockQuantity = product.CurrentStockQuantity,
                ProductGroup = product.ProductGroup,
                Success = true,
                UserRole = "employee",
                Timestamp = timestamp
            };
        }

        _db.ProductScanLogs.Add(scanLog);
        await _db.SaveChangesAsync();

        await _logEventPublisher.PublishAsync(scanLog, scanLog.Success
            ? "product.scan.success"
            : "product.scan.failed");

        if (!scanLog.Success)
            return NotFound(new { message = "Produkt ikke fundet", scanLogId = scanLog.Id });

        return Ok(new
        {
            scanLogId = scanLog.Id,
            product
        });
    }

    [Authorize(Policy = "EmployeeOrCustomer")]   
    [HttpGet("image/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductImageByBarcode(string barcode)
    {
        var product = await _customerService.GetProductByBarcodeAsync(barcode);
        if (product == null)
            return NotFound("Produkt ikke fundet for den angivne barcode");

        var imageUrl = await _productImageService.GetProductImageAsync(product.Uid);
        if (imageUrl == null)
            return NotFound("Produktbillede ikke fundet");

        return Ok(imageUrl);
    }
    
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("available-campaigns")]
    public async Task<IActionResult> GetAllProductCampaigns()
    {
        var campaigns = await _productCampaignService.GetAvailableCampaignsAsync();

        if (campaigns == null || campaigns.Count == 0)
            return NotFound("Ingen produktkampagner fundet");

        return Ok(campaigns);
    }
}
