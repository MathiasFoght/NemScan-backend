using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services.Product
{
    public class ProductCustomerService : IProductCustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly IAmeroAuthService _ameroAuthService;

        public ProductCustomerService(HttpClient httpClient, IAmeroAuthService ameroAuthService)
        {
            _httpClient = httpClient;
            _ameroAuthService = ameroAuthService;
        }

        public async Task<ProductForCustomer?> GetProductByBarcodeAsync(string barcode)
        {
            var token = await _ameroAuthService.GetAccessTokenAsync();

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

            JsonElement? matchingItem = null;
            foreach (var item in items.EnumerateArray())
            {
                var value = item.GetProperty("Value").GetString();
                if (value == barcode)
                {
                    matchingItem = item;
                    break;
                }
            }

            if (matchingItem == null)
                return null;

            var productUid = matchingItem.Value.GetProperty("ProductUid").GetGuid();

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

            return new ProductForCustomer
            {
                ClientUid = root.GetProperty("ClientUid").GetGuid(),
                Uid = root.GetProperty("Uid").GetGuid(),
                Number = root.GetProperty("Number").GetString() ?? "",
                Name = root.GetProperty("Name").GetString() ?? ""
            };
        }
    }
}
