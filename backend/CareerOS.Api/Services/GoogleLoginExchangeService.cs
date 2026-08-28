using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CareerOS.Api.Services;

public class GoogleUserInfo
{
    [JsonPropertyName("sub")]
    public string Sub { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; } = string.Empty;

    [JsonPropertyName("picture")]
    public string Picture { get; set; } = string.Empty;
}

public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

public class ExchangeCodePayload
{
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class GoogleLoginExchangeService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleLoginExchangeService> _logger;

    private static readonly ConcurrentDictionary<string, ExchangeCodePayload> _exchangeCodes = new();

    public GoogleLoginExchangeService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<GoogleLoginExchangeService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public string ClientId => _configuration["Authentication:Google:ClientId"] ?? "PLACEHOLDER_GOOGLE_CLIENT_ID";
    public string ClientSecret => _configuration["Authentication:Google:ClientSecret"] ?? "PLACEHOLDER_GOOGLE_CLIENT_SECRET";
    public string FrontendBaseUrl => _configuration["Authentication:FrontendBaseUrl"] ?? "http://localhost:4200";

    public string CreateExchangeCode(Guid userId)
    {
        var code = Guid.NewGuid().ToString("N");
        var payload = new ExchangeCodePayload
        {
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _exchangeCodes[code] = payload;
        CleanExpiredCodes();
        return code;
    }

    public bool TryConsumeExchangeCode(string code, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(code)) return false;

        if (_exchangeCodes.TryRemove(code, out var payload))
        {
            if (DateTimeOffset.UtcNow - payload.CreatedAt <= TimeSpan.FromSeconds(60))
            {
                userId = payload.UserId;
                return true;
            }
        }

        return false;
    }

    public async Task<GoogleTokenResponse?> ExchangeGoogleCodeAsync(string googleCode, string redirectUri)
    {
        try
        {
            var values = new Dictionary<string, string>
            {
                { "code", googleCode },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to exchange code with Google: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while exchanging Google code");
            return null;
        }
    }

    public async Task<GoogleUserInfo?> GetUserInfoAsync(string accessToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Google user info: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting Google user info");
            return null;
        }
    }

    private static void CleanExpiredCodes()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _exchangeCodes)
        {
            if (now - kvp.Value.CreatedAt > TimeSpan.FromSeconds(60))
            {
                _exchangeCodes.TryRemove(kvp.Key, out _);
            }
        }
    }
}
