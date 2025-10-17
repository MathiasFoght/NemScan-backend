using System.Net.Http.Headers;
using NemScan_API.Interfaces;

namespace NemScan_API.Services.Product;

public class ProductImageService : IProductImageService
{
    private readonly HttpClient _httpClient;
    private readonly IAmeroAuthService _ameroAuthService;

    public ProductImageService(HttpClient httpClient, IAmeroAuthService ameroAuthService)
    {
        _httpClient = httpClient;
        _ameroAuthService = ameroAuthService;
    }

    public async Task<string?> GetProductImageAsync(Guid productUid)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

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

