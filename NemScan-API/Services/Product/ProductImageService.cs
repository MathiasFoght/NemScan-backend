using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services.Product;

public class ProductImageService : IProductImageService
{
    private readonly IAmeroAuthService _ameroAuthService;
    private readonly HttpClient _httpClient;

    public ProductImageService(IAmeroAuthService ameroAuthService, HttpClient httpClient)
    {
        _ameroAuthService = ameroAuthService;
        _httpClient = httpClient;
    }

    public async Task<ProductImage?> GetProductImageAsync(Guid productUid)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.flexpos.com/api/v1.0/product-image/get-file-from-bucket?productUid={productUid}"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("text/plain");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var imageUrl = await response.Content.ReadAsStringAsync();

        return new ProductImage
        {
            ProductUid = productUid,
            ImageUrl = imageUrl
        };
    }

}
