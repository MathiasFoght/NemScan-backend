using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using NemScan_API.Interfaces;

namespace NemScan_API.Services.Amero;

public class AmeroAuthService : IAmeroAuthService, IDisposable
{
    private readonly AmeroAuthConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _currentToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private DateTime _refreshTokenExpiry = DateTime.MinValue;

    public AmeroAuthService(
        IOptions<AmeroAuthConfig> config, 
        IHttpClientFactory httpClientFactory
    )
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_currentToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
            {
                Console.WriteLine("Using existing access token");
                return _currentToken;
            }

            if (!string.IsNullOrEmpty(_currentToken))
            {
                Console.WriteLine("Access token is expired, refreshing...");
            }

            if (!string.IsNullOrEmpty(_refreshToken) && _refreshTokenExpiry > DateTime.UtcNow)
            {
                var refreshedToken = await RefreshAccessTokenAsync();
                if (!string.IsNullOrEmpty(refreshedToken))
                {
                    Console.WriteLine("Access token refreshed");
                    return refreshedToken;
                }
                
                Console.WriteLine("Refresh token is expired, logging in again...");
            }
            else if (!string.IsNullOrEmpty(_refreshToken))
            {
                Console.WriteLine("Refresh token is expired");
            }
            else
            {
                Console.WriteLine("No refresh token found, logging in again...");
            }

            await LoginAsync();
            
            if (string.IsNullOrEmpty(_currentToken))
            {
                throw new InvalidOperationException("Failed to obtain access token");
            }
            return _currentToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoginAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("AmeroAuth");
        
        var authUrl = $"{_config.AuthUrl}/api/v2/auth/login?audience={Uri.EscapeDataString(_config.Audience)}";

        var request = new HttpRequestMessage(HttpMethod.Post, authUrl);

        var basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ApiKey}")
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var body = new Dictionary<string, string>
        {
            { "scope", _config.Scope }
        };
        request.Content = new FormUrlEncodedContent(body);

        var response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Login failed: {response.StatusCode} - {errorContent}"
            );
        }

        var content = await response.Content.ReadAsStringAsync();
        
        var json = JsonDocument.Parse(content);

        _currentToken = json.RootElement.GetProperty("access_token").GetString();
        _refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
        
        var expiryMinutes = _config.AccessTokenExpiryMinutes;
        _tokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        
        _refreshTokenExpiry = DateTime.UtcNow.AddDays(_config.RefreshTokenExpiryDays);
        
        Console.WriteLine("Login successful");
    }

    private async Task<string?> RefreshAccessTokenAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("AmeroAuth");
        
        var refreshUrl = $"{_config.AuthUrl}/api/v2/auth/refresh" +
                        $"?scope={Uri.EscapeDataString(_config.Scope)}" +
                        $"&audience={Uri.EscapeDataString(_config.Audience)}";

        var request = new HttpRequestMessage(HttpMethod.Post, refreshUrl);
        
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "refresh_token", _refreshToken! }
        });
        
        var response = await httpClient.SendAsync(request);

        // If the refresh token is invalid
        if (!response.IsSuccessStatusCode)
        {
            _refreshToken = null;
            _currentToken = null;
            _tokenExpiry = DateTime.MinValue;
            _refreshTokenExpiry = DateTime.MinValue;
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        _currentToken = json.RootElement.GetProperty("access_token").GetString();
        
        if (json.RootElement.TryGetProperty("expires_in", out var expiresInElement))
        {
            var expiresIn = expiresInElement.GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
        }
        else
        {
            var expiryMinutes = _config.AccessTokenExpiryMinutes;
            _tokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        }
        
        Console.WriteLine($"Refresh token successfully refreshed");
        return _currentToken;
    }

    public void Dispose()
    {
        _lock?.Dispose();
    }
}