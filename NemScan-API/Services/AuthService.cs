using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NemScan_API.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GetAccessTokenAsync(string clientId, string clientSecret)
        {
            var byteArray = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var keyValues = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("scope", "product:read client:read") 
            };

            var content = new FormUrlEncodedContent(keyValues);

            var response = await _httpClient.PostAsync("https://auth.flexpos.com/api/v2.0/auth/login", content);

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("StatusCode: " + response.StatusCode);

            if (!response.IsSuccessStatusCode) return null;

            using var jsonDoc = JsonDocument.Parse(responseString);
            if (jsonDoc.RootElement.TryGetProperty("access_token", out var token))
            {
                return token.GetString();
            }

            return null;
        }
    }
}
