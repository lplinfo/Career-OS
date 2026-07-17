using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CareerOS.Api.Contracts;

public class WorkExperienceRequest
{
    public Guid? Id { get; set; }
    [Required, StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [Required, StringLength(160)]
    public string JobTitle { get; set; } = string.Empty;
    [StringLength(4000)]
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int Order { get; set; }
}

public class EducationRequest
{
    public Guid? Id { get; set; }
    [Required, StringLength(200)]
    public string Institution { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Degree { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string FieldOfStudy { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int Order { get; set; }
}

public class CertificationRequest
{
    public Guid? Id { get; set; }
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(200)]
    public string IssuingOrganization { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CredentialId { get; set; }
    public string? CredentialUrl { get; set; }
    public int Order { get; set; }
}

public class CandidateProfileRequest
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;
    [StringLength(200)]
    public string? PreferredName { get; set; }
    [Required, StringLength(160)]
    public string ProfessionalTitle { get; set; } = string.Empty;
    [StringLength(4000)]
    public string? ProfessionalSummary { get; set; }
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public bool OpenToRemoteWork { get; set; }
    public bool OpenToRelocation { get; set; }
    public List<WorkExperienceRequest>? Experiences { get; set; }
    public List<EducationRequest>? Educations { get; set; }
    public List<CertificationRequest>? Certifications { get; set; }
}
