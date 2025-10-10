using System;

namespace NemScan_API.Interfaces
{
    public interface IProductImageService
    {
        Task<string?> GetProductImageAsync(Guid productUid);
    }
}
