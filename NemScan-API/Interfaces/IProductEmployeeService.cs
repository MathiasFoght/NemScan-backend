namespace NemScan_API.Interfaces;

using NemScan_API.Models.DTO.Product;

public interface IProductEmployeeService
{
    Task<ProductForEmployee?> GetProductAsync(Guid productUid);
}