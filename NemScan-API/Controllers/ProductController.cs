using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductCustomerService _customerService;
    private readonly IProductEmployeeService _employeeService;
    private readonly IProductImageService _productImageService;

    public ProductController(
        IProductCustomerService customerService,
        IProductEmployeeService employeeService,
        IProductImageService productImageService)
    {
        _customerService = customerService;
        _employeeService = employeeService;
        _productImageService = productImageService;
    }


    //[Authorize(Roles = "Customer")]
    [HttpGet("customer/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForCustomer(string barcode)
    {
        var product = await _customerService.GetProductByBarcodeAsync(barcode);
        if (product == null)
            return NotFound("Produkt ikke fundet.");

        return Ok(product);
    }

    //[Authorize(Roles = "Basic")]
    [HttpGet("employee/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductForEmployee(string barcode)
    {
        var product = await _employeeService.GetProductByBarcodeAsync(barcode);
        if (product == null)
            return NotFound("Produkt ikke fundet.");

        return Ok(product);
    }

    [HttpGet("image/by-barcode/{barcode}")]
    public async Task<IActionResult> GetProductImageByBarcode(string barcode)
    {
        var product = await _customerService.GetProductByBarcodeAsync(barcode);
        if (product == null)
            return NotFound("Produkt ikke fundet for den angivne barcode.");

        var imageUrl = await _productImageService.GetProductImageAsync(product.Uid);
        if (imageUrl == null)
            return NotFound("Produktbillede ikke fundet.");

        return Ok(imageUrl);
    }
}
