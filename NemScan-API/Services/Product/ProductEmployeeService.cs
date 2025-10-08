using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services.Product
{
    public class ProductEmployeeService : IProductEmployeeService
    {
        private readonly HttpClient _httpClient;
        private readonly IAmeroAuthService _ameroAuthService;

        public ProductEmployeeService(HttpClient httpClient, IAmeroAuthService ameroAuthService)
        {
            _httpClient = httpClient;
            _ameroAuthService = ameroAuthService;
        }

        public async Task<ProductForEmployee?> GetProductByBarcodeAsync(string barcode)
        {
            var token = await _ameroAuthService.GetAccessTokenAsync();

            // Step 1: Find ProductUid ud fra barcode
            var barcodeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.flexpos.com/api/v1.0/barcode?filters[Value][$eq]={barcode}"
            );
            barcodeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var barcodeResponse = await _httpClient.SendAsync(barcodeRequest);
            if (!barcodeResponse.IsSuccessStatusCode)
                return null;

            var barcodeContent = await barcodeResponse.Content.ReadAsStringAsync();
            using var barcodeDoc = JsonDocument.Parse(barcodeContent);

            var items = barcodeDoc.RootElement.GetProperty("Items");
            if (items.GetArrayLength() == 0)
                return null;

            var productUid = items[0].GetProperty("ProductUid").GetGuid();

            // Step 2: Hent produktdata
            var productRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.flexpos.com/api/v1.0/product/{productUid}"
            );
            productRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var productResponse = await _httpClient.SendAsync(productRequest);
            if (!productResponse.IsSuccessStatusCode)
                return null;

            var productContent = await productResponse.Content.ReadAsStringAsync();
            using var productDoc = JsonDocument.Parse(productContent);
            var root = productDoc.RootElement;

            // Step 3: Returner DTO
            return new ProductForEmployee
            {
                ClientUid = root.GetProperty("ClientUid").GetGuid(),
                Uid = root.GetProperty("Uid").GetGuid(),
                Number = root.GetProperty("Number").GetString() ?? "",
                Name = root.GetProperty("Name").GetString() ?? "",
                CurrentStockQuantity = root.TryGetProperty("CurrentStockQuantity", out var stock)
                    ? stock.GetDecimal()
                    : 0
            };
        }
    }
}
