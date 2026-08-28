using CareerOS.Api.Domain;

namespace CareerOS.Api.Services;

public class JwtOptions
{
    public const string SectionName = "JwtOptions";
    public string Issuer { get; set; } = "CareerOS.Api";
    public string Audience { get; set; } = "CareerOS.Frontend";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
}

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(ApplicationUser user);
}
