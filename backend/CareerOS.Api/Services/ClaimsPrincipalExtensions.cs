using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CareerOS.Api.Services;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var subClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(subClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    public static Guid? GetCandidateProfileId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue("candidate_profile_id");
        if (Guid.TryParse(claim, out var profileId))
        {
            return profileId;
        }

        return null;
    }
}
