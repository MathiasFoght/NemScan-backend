namespace NemScan_API.Interfaces;

public interface IProductService
{
    Task<object?> GetProductByBarcodeAsync(string barcode);
}