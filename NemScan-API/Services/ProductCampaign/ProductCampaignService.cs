using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.ProductCampaign;

namespace NemScan_API.Services.ProductCampaign;

public class ProductCampaignService : IProductCampaignService
{
    private readonly HttpClient _httpClient;
    private readonly IAmeroAuthService _ameroAuthService;

    public ProductCampaignService(HttpClient httpClient, IAmeroAuthService ameroAuthService)
    {
        _httpClient = httpClient;
        _ameroAuthService = ameroAuthService;
    }
    
    // Get available campaigns for a product
    public async Task<List<ProductCampaignDTO>> GetAvailableCampaignsAsync()
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();
        var campaigns = new List<ProductCampaignDTO>();

        var url = "https://api.flexpos.com/api/v1.0/product-campaign?offset=0&limit=50";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return campaigns;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        var items = doc.RootElement.GetProperty("Items");
        foreach (var item in items.EnumerateArray())
        {
            var dto = new ProductCampaignDTO
            {
                Uid = item.GetProperty("Uid").GetGuid(),
                Name = item.GetProperty("Name").GetString() ?? "",
                FromDate = item.TryGetProperty("FromDate", out var fromDate)
                           && fromDate.ValueKind == JsonValueKind.String
                           && DateTime.TryParse(fromDate.GetString(), out var parsedFrom)
                    ? parsedFrom
                    : null,
                ToDate = item.TryGetProperty("ToDate", out var toDate)
                         && toDate.ValueKind == JsonValueKind.String
                         && DateTime.TryParse(toDate.GetString(), out var parsedTo)
                    ? parsedTo
                    : null,
                ActivateAtQuantity = item.TryGetProperty("ActivateAtQuantity", out var qty) && qty.ValueKind == JsonValueKind.Number
                    ? qty.GetInt32()
                    : null,
                DiscountInPercentage = item.TryGetProperty("DiscountInPercentage", out var discount)
                                       && discount.ValueKind == JsonValueKind.Number
                    ? discount.GetDecimal()
                    : null,
                ProductsInCampaign = item.TryGetProperty("ProductsInCampaign", out var products)
                    ? products.EnumerateArray().Select(p => p.GetString() ?? "").ToList()
                    : new List<string>()
            };
            
            campaigns.Add(dto);
        }

        return campaigns;
    }
}
