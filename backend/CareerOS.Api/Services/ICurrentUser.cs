namespace CareerOS.Api.Services;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid? CandidateProfileId { get; }
    bool IsAuthenticated { get; }
}
