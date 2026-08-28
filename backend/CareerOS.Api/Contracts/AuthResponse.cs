namespace CareerOS.Api.Contracts;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid CandidateProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
}
