using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CareerOS.Api.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ReturnsTokenWithCorrectClaimsAndExpiration()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "CareerOS.Api",
            Audience = "CareerOS.Frontend",
            SecretKey = "Super_Secret_Test_Key_For_Unit_Testing_256_Bits!",
            AccessTokenMinutes = 15
        });

        var service = new JwtTokenService(options);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CandidateProfileId = Guid.NewGuid()
        };

        var (token, expiresAt) = service.GenerateAccessToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(expiresAt > DateTimeOffset.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("CareerOS.Api", jwtToken.Issuer);
        Assert.Contains("CareerOS.Frontend", jwtToken.Audiences);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        var profileIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "candidate_profile_id")?.Value;

        Assert.Equal(user.Id.ToString(), subClaim);
        Assert.Equal(user.Email, emailClaim);
        Assert.Equal(user.CandidateProfileId.ToString(), profileIdClaim);
    }
}
