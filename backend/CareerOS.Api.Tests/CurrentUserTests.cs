using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CareerOS.Api.Tests;

public class CurrentUserTests
{
    [Fact]
    public void ClaimsPrincipalExtensions_ParsesUserIdAndCandidateProfileId()
    {
        var expectedUserId = Guid.NewGuid();
        var expectedProfileId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, expectedUserId.ToString()),
            new Claim("candidate_profile_id", expectedProfileId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        Assert.Equal(expectedUserId, principal.GetUserId());
        Assert.Equal(expectedProfileId, principal.GetCandidateProfileId());
    }

    [Fact]
    public void CurrentUser_ReadsClaimsFromHttpContext()
    {
        var expectedUserId = Guid.NewGuid();
        var expectedProfileId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim("candidate_profile_id", expectedProfileId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(expectedUserId, currentUser.UserId);
        Assert.Equal(expectedProfileId, currentUser.CandidateProfileId);
    }
}
