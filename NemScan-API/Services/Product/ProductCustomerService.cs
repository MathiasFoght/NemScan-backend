using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;

namespace NemScan_API.Services;

public class ProductCustomerService : IProductCustomerService
{
    private readonly IAmeroAuthService _ameroAuthService;
    private readonly HttpClient _httpClient;

    public ProductCustomerService(IAmeroAuthService ameroAuthService, HttpClient httpClient)
    {
        _ameroAuthService = ameroAuthService;
        _httpClient = httpClient;
    }

    public async Task<ProductForCustomer?> GetProductAsync(Guid productUid)
{
    var token = await _ameroAuthService.GetAccessTokenAsync();

    var request = new HttpRequestMessage(
    HttpMethod.Get,
    $"https://api.flexpos.com/api/v1/product/{productUid}" 
);


    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await _httpClient.SendAsync(request);

    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Status: {response.StatusCode}");
    Console.WriteLine($"Content: {content}");

    if (!response.IsSuccessStatusCode)
        return null;

    using var doc = JsonDocument.Parse(content);
    var root = doc.RootElement;

    return new ProductForCustomer
    {
        ClientUid = root.GetProperty("ClientUid").GetGuid(),
        Uid = root.GetProperty("Uid").GetGuid(),
        Number = root.GetProperty("Number").GetString() ?? "",
        Name = root.GetProperty("Name").GetString() ?? ""
    };
}

}
