namespace NemScan_API.Interfaces
{
    public interface IProductImageService
    {
        Task<string?> GetProductImageByBarcodeAsync(Guid productUid);
    }
}
