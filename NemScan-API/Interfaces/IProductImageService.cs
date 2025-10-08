namespace NemScan_API.Interfaces;

using NemScan_API.Models.DTO.Product;

public interface IProductImageService
{
    Task<ProductImage?> GetProductImageAsync(Guid productUid);
}
