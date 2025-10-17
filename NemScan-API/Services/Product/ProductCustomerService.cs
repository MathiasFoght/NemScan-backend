using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services.Product;
    
public class ProductCustomerService : IProductCustomerService
{
    private readonly HttpClient _httpClient;
    private readonly IAmeroAuthService _ameroAuthService;
    private readonly IProductCampaignService _productCampaignService;

    public ProductCustomerService(HttpClient httpClient, IAmeroAuthService ameroAuthService, IProductCampaignService productCampaignService)
    {
        _httpClient = httpClient;
        _ameroAuthService = ameroAuthService;
        _productCampaignService = productCampaignService;
    }

    public async Task<ProductCustomerDTO?> GetProductByBarcodeAsync(string barcode)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

        // Find product UID from barcode
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

        // Get productinfo from product UID
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

        var dto = new ProductCustomerDTO
        {
            Uid = productUid,
            ProductNumber = root.GetProperty("Number").GetString() ?? "",
            ProductName = root.GetProperty("Name").GetString() ?? ""
        };

        // Get CurrentSalesPrice from product UID
        var priceRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.flexpos.com/api/v1.0/product/{productUid}?fields=CurrentSalesPrice"
        );
        priceRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var priceResponse = await _httpClient.SendAsync(priceRequest);
        if (priceResponse.IsSuccessStatusCode)
        {
            var priceContent = await priceResponse.Content.ReadAsStringAsync();
            using var priceDoc = JsonDocument.Parse(priceContent);
            var priceRoot = priceDoc.RootElement;

            JsonElement priceElement = priceRoot;
            if (priceRoot.TryGetProperty("Item", out var itemProp))
                priceElement = itemProp;

            if (priceElement.TryGetProperty("CurrentSalesPrice", out var priceValue) &&
                priceValue.ValueKind == JsonValueKind.Number)
            {
                dto.CurrentSalesPrice = priceValue.GetDecimal();
            }
        }
        
        // Get DisplayProductGroupUid from product UID
        var groupUidUrl = $"https://api.flexpos.com/api/v1.0/product/{productUid}?fields=DisplayProductGroupUid";
        var groupUidRequest = new HttpRequestMessage(HttpMethod.Get, groupUidUrl);
        groupUidRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var groupUidResponse = await _httpClient.SendAsync(groupUidRequest);
        if (!groupUidResponse.IsSuccessStatusCode)
        {
            return null;       
        }

        string? displayGroupUid = null;
        var groupUidContent = await groupUidResponse.Content.ReadAsStringAsync();
        using var groupUidDoc = JsonDocument.Parse(groupUidContent);
        var groupUidRoot = groupUidDoc.RootElement;

        if (groupUidRoot.TryGetProperty("DisplayProductGroupUid", out var groupUidProp))
        {
            displayGroupUid = groupUidProp.GetString();
        }

        // Get display group name from UID with name lookup
        if (!string.IsNullOrEmpty(displayGroupUid))
        {
            var displayGroupUrl = $"https://api.flexpos.com/api/v2.0/display-group/{displayGroupUid}?fields=Name";
            var displayGroupRequest = new HttpRequestMessage(HttpMethod.Get, displayGroupUrl);
            displayGroupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var displayGroupResponse = await _httpClient.SendAsync(displayGroupRequest);
            if (displayGroupResponse.IsSuccessStatusCode)
            {
                var groupContent = await displayGroupResponse.Content.ReadAsStringAsync();
                using var groupDoc = JsonDocument.Parse(groupContent);
                var groupRoot = groupDoc.RootElement;

                if (groupRoot.TryGetProperty("Name", out var nameProp))
                {
                    dto.ProductGroup = nameProp.GetString();
                }
            }
        }
        
        // Get campaigns for product
        var campaigns = await _productCampaignService.GetAvailableCampaignsAsync();
        dto.Campaigns = campaigns
            .Where(c => c.ProductsInCampaign.Contains(dto.ProductNumber))
            .ToList();
        
        return dto;
    }
}
