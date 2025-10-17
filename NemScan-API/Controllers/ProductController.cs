using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;
using NemScan_API.Models.Events;

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

    public ProductController(
        IProductCustomerService customerService,
        IProductEmployeeService employeeService,
        IProductImageService productImageService,
        ILogEventPublisher logEventPublisher,
        IProductCampaignService productCampaignService)
    {
        _customerService = customerService;
        _employeeService = employeeService;
        _productImageService = productImageService;
        _logEventPublisher = logEventPublisher;
        _productCampaignService = productCampaignService;
    }
    
    [Authorize(Policy = "CustomerOnly")]   
    [HttpGet("customer/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForCustomer(string barcode)
    {
        var product = await _customerService.GetProductByBarcodeAsync(barcode);
        if (product == null)
        {
            await _logEventPublisher.PublishAsync(new ProductScanLogEvent
            {
                ProductNumber = barcode,
                Success = false,
                UserRole = "customer",
                FailureReason = "Product not found",
                Timestamp = DateTime.UtcNow
            }, "product.scan.failed");

            return NotFound($"Produkt ikke fundet");
        }
        
        await _logEventPublisher.PublishAsync(new ProductScanLogEvent
        {
            ProductNumber = product.ProductNumber,
            ProductName = product.ProductName,
            CurrentSalesPrice = product.CurrentSalesPrice,
            ProductGroup = product.ProductGroup,
            Success = true,
            UserRole = "customer",
            Timestamp = DateTime.UtcNow
        }, "product.scan.success");
        
        return Ok(product);
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("employee/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForEmployee(string barcode)
    {
        var product = await _employeeService.GetProductByBarcodeAsync(barcode);
        if (product == null)
        {
            await _logEventPublisher.PublishAsync(new ProductScanLogEvent
            {
                ProductNumber = barcode,
                Success = false,
                FailureReason = "Product not found",
                Timestamp = DateTime.UtcNow
            }, "product.scan.failed");

            return NotFound("Produkt ikke fundet");
        }
        
        await _logEventPublisher.PublishAsync(new ProductScanLogEvent
        {
            ProductNumber = product.ProductNumber,
            ProductName = product.ProductName,
            CurrentSalesPrice = product.CurrentSalesPrice,
            CurrentStockQuantity = product.CurrentStockQuantity,
            ProductGroup = product.ProductGroup,
            Success = true,
            UserRole = "employee",
            Timestamp = DateTime.UtcNow
        }, "product.scan.success");

        return Ok(product);
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
