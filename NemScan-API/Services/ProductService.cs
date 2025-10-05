using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Models;

namespace NemScan_API.Services;

public class ProductService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public ProductService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

  /*   public async Task<Product?> GetProductAsync(string productUid, string clientId, string clientSecret)
    {
        var token = await _authService.GetAccessTokenAsync(clientId, clientSecret);
        if (token == null) return null;

        var url = $"https://api.flexpos.com/api/v1.0/product/{productUid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    } */
}
