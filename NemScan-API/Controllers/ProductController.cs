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

    [HttpGet("customer/{productUid}")]
    public async Task<IActionResult> GetProductForCustomer(Guid productUid)
    {
        var product = await _customerService.GetProductAsync(productUid);
        if (product == null)
            return NotFound("Produkt ikke fundet.");

        return Ok(product);
    }

    [HttpGet("employee/{productUid}")]
    public async Task<IActionResult> GetProductForEmployee(Guid productUid)
    {
        var product = await _employeeService.GetProductAsync(productUid);
        if (product == null)
            return NotFound("Produkt ikke fundet.");

        return Ok(product);
    }

    [HttpGet("image/{productUid}")]
    public async Task<IActionResult> GetProductImage(Guid productUid)
    {
        var image = await _productImageService.GetProductImageAsync(productUid);
        if (image == null)
            return NotFound("Produktbillede ikke fundet.");

        return Ok(image);
    }


}
