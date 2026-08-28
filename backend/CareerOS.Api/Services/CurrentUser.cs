using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CareerOS.Api.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId => User?.GetUserId() ?? Guid.Empty;

    public Guid? CandidateProfileId => User?.GetCandidateProfileId();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
