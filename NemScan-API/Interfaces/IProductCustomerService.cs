namespace NemScan_API.Interfaces;

using NemScan_API.Models.DTO.Products;

public interface IProductCustomerService
{
    Task<CustomerDTO?> GetProductAsync(Guid productUid);
}