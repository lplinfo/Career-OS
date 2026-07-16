using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CareerOS.Api.Contracts;

public record CandidateProfileRequest(
    [property: Required, StringLength(200)] string FullName,
    [property: StringLength(200)] string? PreferredName,
    [property: Required, StringLength(160)] string ProfessionalTitle,
    [property: StringLength(4000)] string? ProfessionalSummary,
    [property: Required, EmailAddress, StringLength(320)] string Email,
    string? Phone, string? City, string? Region, string? Country,
    bool OpenToRemoteWork, bool OpenToRelocation,
    List<WorkExperienceRequest>? WorkExperiences = null,
    List<EducationRequest>? EducationHistory = null,
    List<CertificationRequest>? Certifications = null);

public record WorkExperienceRequest(string Company, string Role, DateOnly? StartDate, DateOnly? EndDate, bool IsCurrent, string? Description, int DisplayOrder);
public record EducationRequest(string Institution, string Course, string? Degree, DateOnly? CompletionDate, int DisplayOrder);
public record CertificationRequest(string Name, string? Issuer, DateOnly? IssuedAt, string? CredentialUrl, int DisplayOrder);
