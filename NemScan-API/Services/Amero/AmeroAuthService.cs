using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using NemScan_API.Interfaces;

namespace NemScan_API.Services.Amero;

public class AmeroAuthService : IAmeroAuthService
{
    private readonly AmeroAuthConfig _config;
    private readonly HttpClient _httpClient;

    private string? _currentToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public AmeroAuthService(IOptions<AmeroAuthConfig> config, HttpClient httpClient)
    {
        _config = config.Value;
        _httpClient = httpClient;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // Hvis token stadig er gyldigt
        if (!string.IsNullOrEmpty(_currentToken) && _tokenExpiry > DateTime.UtcNow)
        {
            return _currentToken!;
        }

        // Brug refresh token
        if (!string.IsNullOrEmpty(_refreshToken))
        {
            var refreshToken = await RefreshAccessTokenAsync();
            if (!string.IsNullOrEmpty(refreshToken))
                return refreshToken;
        }

        // Nyt login
        await LoginAsync();
        return _currentToken!;
    }

    private async Task LoginAsync()
    {
        var authUrl = $"{_config.AuthUrl}/api/v2/auth/login?audience={_config.Audience}";

        var request = new HttpRequestMessage(HttpMethod.Post, authUrl);

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ApiKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var body = new Dictionary<string, string>
        {
            { "scope", _config.Scope }
        };
        request.Content = new FormUrlEncodedContent(body);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        _currentToken = json.RootElement.GetProperty("access_token").GetString();
        _refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
        _tokenExpiry = DateTime.UtcNow.AddMinutes(50);
    }

    private async Task<string?> RefreshAccessTokenAsync()
    {
        var refreshUrl = $"{_config.AuthUrl}/api/v2/auth/refresh";

        var request = new HttpRequestMessage(HttpMethod.Post, refreshUrl);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "refresh_token", _refreshToken! }
        });

        var response = await _httpClient.SendAsync(request);

        // Hvis refresh fejler, nulstil og login igen
        if (!response.IsSuccessStatusCode)
        {
            _refreshToken = null;
            _currentToken = null;
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        _currentToken = json.RootElement.GetProperty("access_token").GetString();
        _refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

        return _currentToken;
    }
}

