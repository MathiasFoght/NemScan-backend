using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;
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
    private readonly IProductAllProductsService _productAllProductsService;
    public record DeviceRequest(string DeviceId);

    public ProductController(
        IProductCustomerService customerService,
        IProductEmployeeService employeeService,
        IProductImageService productImageService,
        ILogEventPublisher logEventPublisher,
        IProductCampaignService productCampaignService,
        IProductAllProductsService productAllProductsService)
    {
        _customerService = customerService;
        _employeeService = employeeService;
        _productImageService = productImageService;
        _logEventPublisher = logEventPublisher;
        _productCampaignService = productCampaignService;
        _productAllProductsService = productAllProductsService;
    }
    
    [Authorize(Policy = "CustomerOnly")]   
    [HttpPost("customer/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForCustomer(string barcode, [FromBody] DeviceRequest request)
    {
        var timestamp = DateTime.UtcNow;
        var product = await _customerService.GetProductByBarcodeAsync(barcode);

        ProductScanLogEvent scanLog;

        if (product == null)
        {
            scanLog = new ProductScanLogEvent
            {
                Id = Guid.NewGuid(),
                DeviceId = request.DeviceId,
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
                DeviceId = request.DeviceId,
                ProductNumber = product.ProductNumber,
                ProductName = product.ProductName,
                ProductGroup = product.ProductGroup,
                Success = true,
                UserRole = "customer",
                Timestamp = timestamp
            };
        }

        await _logEventPublisher.PublishAsync(scanLog, scanLog.Success
            ? "product.scan.success"
            : "product.scan.failed");

        if (!scanLog.Success)
            return NotFound(new { message = "Product not found", scanLogId = scanLog.Id });

        return Ok(new
        {
            scanLogId = scanLog.Id,
            product
        });
    }

    [Authorize(Policy = "EmployeeOnly")]
    [HttpPost("employee/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForEmployee(string barcode, [FromBody] DeviceRequest request)
    {
        var product = await _employeeService.GetProductByBarcodeAsync(barcode);
        var timestamp = DateTime.UtcNow;

        ProductScanLogEvent scanLog;

        if (product == null)
        {
            scanLog = new ProductScanLogEvent
            {
                Id = Guid.NewGuid(),
                DeviceId = request.DeviceId,
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
                DeviceId = request.DeviceId,
                ProductNumber = product.ProductNumber,
                ProductName = product.ProductName,
                ProductGroup = product.ProductGroup,
                Success = true,
                UserRole = "employee",
                Timestamp = timestamp
            };
        }

        await _logEventPublisher.PublishAsync(scanLog, scanLog.Success
            ? "product.scan.success"
            : "product.scan.failed");

        if (!scanLog.Success)
            return NotFound(new { message = "Product not found", scanLogId = scanLog.Id });

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
        var userType = User.FindFirst("userType")?.Value;

        if (string.IsNullOrEmpty(userType))
            return Forbid("User type not specified in token");

        SimpleProductDTO? product = null;

        if (userType.Equals("employee", StringComparison.OrdinalIgnoreCase))
        {
            var empProduct = await _employeeService.GetProductByBarcodeAsync(barcode);
            if (empProduct != null)
                product = new SimpleProductDTO { Uid = empProduct.Uid };
        }
        else if (userType.Equals("customer", StringComparison.OrdinalIgnoreCase))
        {
            var custProduct = await _customerService.GetProductByBarcodeAsync(barcode);
            if (custProduct != null)
                product = new SimpleProductDTO { Uid = custProduct.Uid };
        }

        if (product == null)
            return NotFound("Product not found for the specified barcode");

        var imageUrl = await _productImageService.GetProductImageByBarcodeAsync(product.Uid);
        if (imageUrl == null)
            return NotFound("Product image not found");

        return Ok(imageUrl);
    }
    
    [Authorize(Policy = "EmployeeOnly")]
    [HttpGet("available-campaigns")]
    public async Task<IActionResult> GetAllProductCampaigns()
    {
        var campaigns = await _productCampaignService.GetAvailableCampaignsAsync();

        if (campaigns == null || campaigns.Count == 0)
            return NotFound("No campaigns found");

        return Ok(campaigns);
    }
    
    [Authorize(Policy = "EmployeeOrCustomer")]
    [HttpGet("all-products")]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _productAllProductsService.GetAllProductsAsync();

        if (products == null || products.Count == 0)
            return NotFound("No products to retrieve");
        
        return Ok(products);
    }

    
    
}
