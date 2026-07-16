using System.ComponentModel.DataAnnotations;

namespace CareerOS.Api.Contracts;

public record CandidateProfileRequest(
    [property: Required, StringLength(200)] string FullName,
    [property: StringLength(200)] string? PreferredName,
    [property: Required, StringLength(160)] string ProfessionalTitle,
    [property: StringLength(4000)] string? ProfessionalSummary,
    [property: Required, EmailAddress, StringLength(320)] string Email,
    string? Phone, string? City, string? Region, string? Country,
    bool OpenToRemoteWork, bool OpenToRelocation);
