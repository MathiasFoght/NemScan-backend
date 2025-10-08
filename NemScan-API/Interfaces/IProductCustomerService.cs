namespace NemScan_API.Interfaces;

using NemScan_API.Models.DTO.Product;

public interface IProductCustomerService
{
    Task<ProductForCustomer?> GetProductAsync(Guid productUid);
}