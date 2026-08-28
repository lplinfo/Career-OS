using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CareerOS.Api.Contracts;

public class CandidateProfileRequest
{
    [Required, StringLength(200)] public string FullName { get; set; } = string.Empty;
    [StringLength(200)] public string? PreferredName { get; set; }
    [Required, StringLength(160)] public string ProfessionalTitle { get; set; } = string.Empty;
    [StringLength(4000)] public string? ProfessionalSummary { get; set; }
    [Required, EmailAddress, StringLength(320)] public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public bool OpenToRemoteWork { get; set; }
    public bool OpenToRelocation { get; set; }
    public List<WorkExperienceRequest> WorkExperiences { get; set; } = [];
    public List<EducationRequest> EducationHistory { get; set; } = [];
    public List<CertificationRequest> Certifications { get; set; } = [];
}

public record WorkExperienceRequest(string Company, string Role, DateOnly? StartDate, DateOnly? EndDate, bool IsCurrent, string? Description, int DisplayOrder);
public record EducationRequest(string Institution, string Course, string? Degree, DateOnly? CompletionDate, int DisplayOrder);
public record CertificationRequest(string Name, string? Issuer, DateOnly? IssuedAt, string? CredentialUrl, int DisplayOrder);
