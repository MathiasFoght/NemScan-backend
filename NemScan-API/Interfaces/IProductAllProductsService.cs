using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Interfaces;

public interface IProductAllProductsService
{
    Task<List<AllProductsDTO>> GetAllProductsAsync();
}