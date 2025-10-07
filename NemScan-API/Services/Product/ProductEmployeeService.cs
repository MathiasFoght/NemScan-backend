using System.Net.Http.Headers;
using System.Text.Json;
using NemScan_API.Models.DTO.Products;
using NemScan_API.Interfaces;

namespace NemScan_API.Services;

public class ProductEmployeeService : IProductEmployeeService
{
    private readonly IAmeroAuthService _ameroAuthService;
    private readonly HttpClient _httpClient;

    public ProductEmployeeService(IAmeroAuthService ameroAuthService, HttpClient httpClient)
    {
        _ameroAuthService = ameroAuthService;
        _httpClient = httpClient;
    }

    public async Task<EmployeeDTO?> GetProductAsync(Guid productUid)
    {
        var token = await _ameroAuthService.GetAccessTokenAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.flexpos.com/api/v1/product/{productUid}"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        return new EmployeeDTO
        {
            ClientUid = root.GetProperty("ClientUid").GetGuid(),
            Uid = root.GetProperty("Uid").GetGuid(),
            Number = root.GetProperty("Number").GetString() ?? "",
            Name = root.GetProperty("Name").GetString() ?? "",
            CurrentStockQuantity = root.GetProperty("CurrentStockQuantity").GetDecimal()
        };
    }
}
