using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services.AllProducts;

public class AllProductsServiceService : IProductAllProductsService
{
    private readonly HttpClient _httpClient;
    private readonly IAmeroAuthService _ameroAuthService;

    public AllProductsServiceService(HttpClient httpClient, IAmeroAuthService ameroAuthService)
    {
        _httpClient = httpClient;
        _ameroAuthService = ameroAuthService;
    }

    public async Task<List<AllProductsDTO>> GetAllProductsAsync()
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();
        var products = new List<AllProductsDTO>();

        var requestUrl =
            "https://api.flexpos.com/api/v1.0/product?offset=0&limit=500" +
            "&fields=Uid&fields=Number&fields=Name";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return products;

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("Items", out var items))
            return products;

        foreach (var item in items.EnumerateArray())
        {
            var uid = item.GetProperty("Uid").GetString();
            var number = item.GetProperty("Number").GetString();
            var name = item.GetProperty("Name").GetString();

            string? imageUrl = null;

            if (!string.IsNullOrEmpty(uid))
            {
                imageUrl = await GetProductImageAsync(uid, token);
            }

            products.Add(new AllProductsDTO
            {
                ProductNumber = number ?? "",
                Name = name ?? "",
                ImageUrl = imageUrl
            });
        }

        return products;
    }

    private async Task<string?> GetProductImageAsync(string productUid, string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.flexpos.com/api/v1.0/product-image/get-file-from-bucket?productUid={productUid}"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var imageUrl = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
