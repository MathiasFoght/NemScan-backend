using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NemScan_API.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetAccessTokenAsync(string clientId, string clientSecret)
    {
        var url = "https://auth.flexpos.com/api/v2/auth/login?audience=https://api.flexpos.com";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var formData = new Dictionary<string, string>
        {
            { "scope", "product:read" }
        };
        request.Content = new FormUrlEncodedContent(formData);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("access_token").GetString();
    }
}
