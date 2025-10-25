using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services.ProductScan;

public class ProductEmployeeService : IProductEmployeeService
{
    private readonly HttpClient _httpClient;
    private readonly IAmeroAuthService _ameroAuthService;
    private readonly IProductCampaignService _productCampaignService;

    public ProductEmployeeService(HttpClient httpClient, IAmeroAuthService ameroAuthService, IProductCampaignService productCampaignService)
    {
        _httpClient = httpClient;
        _ameroAuthService = ameroAuthService;
        _productCampaignService = productCampaignService;

    }

    public async Task<ProductEmployeeDTO?> GetProductByBarcodeAsync(string barcode)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

        // Get product UID from barcode
        var barcodeUrl = $"https://api.flexpos.com/api/v1.0/barcode?filters[Value][$eq]={barcode}";
        var barcodeRequest = new HttpRequestMessage(HttpMethod.Get, barcodeUrl);
        barcodeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var barcodeResponse = await _httpClient.SendAsync(barcodeRequest);
        if (!barcodeResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var barcodeContent = await barcodeResponse.Content.ReadAsStringAsync();
        using var barcodeDoc = JsonDocument.Parse(barcodeContent);
        var items = barcodeDoc.RootElement.GetProperty("Items");

        if (items.GetArrayLength() == 0)
        {
            return null;
        }

        var matchingItem = items.EnumerateArray().FirstOrDefault(i => i.GetProperty("Value").GetString() == barcode);
        if (matchingItem.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var productUid = matchingItem.GetProperty("ProductUid").GetGuid();


        // Get product info from product UID
        var productInfoUrl = $"https://api.flexpos.com/api/v1.0/product/{productUid}";
        var productInfoRequest = new HttpRequestMessage(HttpMethod.Get, productInfoUrl);
        productInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productInfoResponse = await _httpClient.SendAsync(productInfoRequest);
        if (!productInfoResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var productInfoContent = await productInfoResponse.Content.ReadAsStringAsync();
        using var productInfoDoc = JsonDocument.Parse(productInfoContent);
        var productRoot = productInfoDoc.RootElement;

        var dto = new ProductEmployeeDTO
        {
            Uid = productUid,
            ProductNumber = productRoot.GetProperty("Number").GetString() ?? "",
            ProductName = productRoot.GetProperty("Name").GetString() ?? ""
        };


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
        
        // Get brand UID from product UID
        var brandUidUrl = $"https://api.flexpos.com/api/v1.0/product/{productUid}?fields=brandUid";
        var brandUidRequest = new HttpRequestMessage(HttpMethod.Get, brandUidUrl);
        brandUidRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var brandUidResponse = await _httpClient.SendAsync(brandUidRequest);
        if (!brandUidResponse.IsSuccessStatusCode)
        {
            return null;      
        }

        string? brandUid = null;
        var brandUidContent = await brandUidResponse.Content.ReadAsStringAsync();
        using var brandUidDoc = JsonDocument.Parse(brandUidContent);
        var brandUidRoot = brandUidDoc.RootElement;

        if (brandUidRoot.TryGetProperty("BrandUid", out var brandUidProp))
        {
            brandUid = brandUidProp.GetString();
        }
        
        // Get brand name from UID with name lookup
        if (!string.IsNullOrEmpty(brandUid))
        {
            var brandGroupUrl = $"https://api.flexpos.com/api/v1.0/brand/{brandUid}?returnFields=Name";
            var brandRequest = new HttpRequestMessage(HttpMethod.Get, brandGroupUrl);
            brandRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var brandResponse = await _httpClient.SendAsync(brandRequest);
            if (brandResponse.IsSuccessStatusCode)
            {
                var brandContent = await brandResponse.Content.ReadAsStringAsync();
                using var brandDoc = JsonDocument.Parse(brandContent);
                var brandRoot = brandDoc.RootElement;

                if (brandRoot.TryGetProperty("Name", out var nameProp))
                {
                    dto.ProductBrand = nameProp.GetString();
                    Console.WriteLine($"[INFO] Brand name: {dto.ProductBrand}");
                }
            }
        }


        // Get CurrentSalesPrice from product UID
        var priceUrl = $"https://api.flexpos.com/api/v1.0/product/{productUid}?fields=CurrentSalesPrice";
        var priceRequest = new HttpRequestMessage(HttpMethod.Get, priceUrl);
        priceRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var priceResponse = await _httpClient.SendAsync(priceRequest);
        var priceContent = await priceResponse.Content.ReadAsStringAsync();
        using var priceDoc = JsonDocument.Parse(priceContent);
        var priceRoot = priceDoc.RootElement;

        if (priceRoot.TryGetProperty("CurrentSalesPrice", out var priceValue) &&
            priceValue.ValueKind == JsonValueKind.Number)
        {
            dto.CurrentSalesPrice = priceValue.GetDecimal();
        }

        // Get CurrentStockQuantity from product UID
        var stockUrl = $"https://api.flexpos.com/api/v1.0/product/{productUid}?fields=CurrentStockQuantity";
        var stockRequest = new HttpRequestMessage(HttpMethod.Get, stockUrl);
        stockRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var stockResponse = await _httpClient.SendAsync(stockRequest);
        var stockContent = await stockResponse.Content.ReadAsStringAsync();
        using var stockDoc = JsonDocument.Parse(stockContent);
        var stockRoot = stockDoc.RootElement;

        if (stockRoot.TryGetProperty("CurrentStockQuantity", out var stockValue) &&
            stockValue.ValueKind == JsonValueKind.Number)
        {
            dto.CurrentStockQuantity = stockValue.GetDecimal();
        }
        
        // Get campaigns for product
        var campaigns = await _productCampaignService.GetAvailableCampaignsAsync();
        dto.Campaigns = campaigns
            .Where(c => c.ProductsInCampaign.Contains(dto.ProductNumber))
            .ToList();
        
        // Return DTO
        return dto;
    }
    
    public async Task<List<LowStockProductDTO>> GetLowStockProductsAsync(double minThreshold = 100)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

        var requestUrl =
            $"https://api.flexpos.com/api/v1.0/product?offset=0&limit=500" +
            $"&filters[CurrentStockQuantity][$lt]={minThreshold}" +
            $"&fields=CurrentStockQuantity&fields=Number&fields=Name&fields=DisplayProductGroupUid";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<LowStockProductDTO>();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("Items", out var items))
            return new List<LowStockProductDTO>();

        var lowStockProducts = new List<LowStockProductDTO>();

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("CurrentStockQuantity", out var stockProp) ||
                stockProp.ValueKind != JsonValueKind.Number)
                continue;

            var stock = stockProp.GetDecimal();

            if (stock >= (decimal)minThreshold)
                continue;

            // UID for DisplayGroup
            string? groupUid = null;
            if (item.TryGetProperty("DisplayProductGroupUid", out var groupUidProp))
                groupUid = groupUidProp.GetString();

            string productGroupName = "Unknown";

            // Get DisplayGroup name using groupUid
            if (!string.IsNullOrEmpty(groupUid))
            {
                var groupUrl = $"https://api.flexpos.com/api/v2.0/display-group/{groupUid}?fields=Name";
                var groupRequest = new HttpRequestMessage(HttpMethod.Get, groupUrl);
                groupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var groupResponse = await _httpClient.SendAsync(groupRequest);
                if (groupResponse.IsSuccessStatusCode)
                {
                    var groupContent = await groupResponse.Content.ReadAsStringAsync();
                    using var groupDoc = JsonDocument.Parse(groupContent);

                    if (groupDoc.RootElement.TryGetProperty("Name", out var nameProp))
                        productGroupName = nameProp.GetString() ?? "Unknown";
                }
            }

            var dto = new LowStockProductDTO
            {
                ProductNumber = item.GetProperty("Number").GetString() ?? "",
                ProductName = item.GetProperty("Name").GetString() ?? "",
                ProductGroup = productGroupName,
                CurrentStockQuantity = stock
            };

            lowStockProducts.Add(dto);
        }

        return lowStockProducts
            .OrderBy(p => p.CurrentStockQuantity)
            .Take(5)
            .ToList();
    }
}

