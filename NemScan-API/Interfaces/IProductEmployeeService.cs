using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Interfaces
{
    public interface IProductEmployeeService
    {
        Task<ProductEmployeeDTO?> GetProductByBarcodeAsync(string barcode);
    }
}