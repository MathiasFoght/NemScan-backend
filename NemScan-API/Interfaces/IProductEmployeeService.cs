namespace NemScan_API.Interfaces;

using NemScan_API.Models.DTO.Products;

public interface IProductEmployeeService
{
    Task<EmployeeDTO?> GetProductAsync(Guid productUid);
}