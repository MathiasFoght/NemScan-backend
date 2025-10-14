using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Events;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductCustomerService _customerService;
    private readonly IProductEmployeeService _employeeService;
    private readonly IProductImageService _productImageService;
    private readonly ILogEventPublisher _logEventPublisher;

    public ProductController(
        IProductCustomerService customerService,
        IProductEmployeeService employeeService,
        IProductImageService productImageService,
        ILogEventPublisher logEventPublisher)
    {
        _customerService = customerService;
        _employeeService = employeeService;
        _productImageService = productImageService;
        _logEventPublisher = logEventPublisher;   
    }


    //[Authorize(Roles = "Customer")]   
    [HttpGet("customer/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForCustomer(string barcode)
    {
        var product = await _customerService.GetProductByBarcodeAsync(barcode);
        if (product == null)
        {
            await _logEventPublisher.PublishAsync(new ProductLogEvent
            {
                ProductNumber = barcode,
                Success = false,
                UserRole = "customer",
                FailureReason = "Produkt ikke fundet",
                Timestamp = DateTime.UtcNow
            }, "product.scan.failed");

            return NotFound($"Produkt ikke fundet");
        }
        
        await _logEventPublisher.PublishAsync(new ProductLogEvent
        {
            ProductNumber = product.Number,
            ProductName = product.Name,
            DisplayProductGroupUid = product.DisplayProductGroupUid,
            CurrentSalesPrice = product.CurrentSalesPrice,
            Success = true,
            UserRole = "customer",
            Timestamp = DateTime.UtcNow
        }, "product.scan.success");
        
        return Ok(product);
    }

    //[Authorize(Roles = "Basic")]
    [HttpGet("employee/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForEmployee(string barcode)
    {
        var product = await _employeeService.GetProductByBarcodeAsync(barcode);
        if (product == null)
        {
            await _logEventPublisher.PublishAsync(new ProductLogEvent
            {
                ProductNumber = barcode,
                Success = false,
                FailureReason = "Product not found",
                Timestamp = DateTime.UtcNow
            }, "product.scan.failed");

            return NotFound("Produkt ikke fundet");
        }
        
        await _logEventPublisher.PublishAsync(new ProductLogEvent
        {
            ProductNumber = product.Number,
            ProductName = product.Name,
            DisplayProductGroupUid = product.DisplayProductGroupUid,
            CurrentSalesPrice = product.CurrentSalesPrice,
            CurrentStockQuantity = product.CurrentStockQuantity,
            Success = true,
            UserRole = "employee",
            Timestamp = DateTime.UtcNow
        }, "product.scan.success");

        return Ok(product);
    }

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
}
