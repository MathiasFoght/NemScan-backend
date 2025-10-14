using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Interfaces
{
    public interface IProductCustomerService
    {
        Task<ProductCustomerDTO?> GetProductByBarcodeAsync(string barcode);
    }
}