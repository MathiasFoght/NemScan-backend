using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Interfaces
{
    public interface IProductCustomerService
    {
        Task<ProductForCustomer?> GetProductByBarcodeAsync(string barcode);
    }
}