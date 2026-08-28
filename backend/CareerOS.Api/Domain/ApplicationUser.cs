using Microsoft.AspNetCore.Identity;

namespace CareerOS.Api.Domain;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid CandidateProfileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LegacyPasswordHash { get; set; }
}
