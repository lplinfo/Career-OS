using System;
using System.Collections.Generic;

namespace CareerOS.Api.Domain;

public class CandidateProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string ProfessionalTitle { get; set; } = string.Empty;
    public string? ProfessionalSummary { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public bool OpenToRemoteWork { get; set; }
    public bool OpenToRelocation { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<WorkExperience> WorkExperiences { get; set; } = [];
    public List<Education> EducationHistory { get; set; } = [];
    public List<Certification> Certifications { get; set; } = [];
}

public class WorkExperience { public Guid Id { get; set; } = Guid.NewGuid(); public Guid CandidateProfileId { get; set; } public string Company { get; set; } = string.Empty; public string Role { get; set; } = string.Empty; public DateOnly? StartDate { get; set; } public DateOnly? EndDate { get; set; } public bool IsCurrent { get; set; } public string? Description { get; set; } public int DisplayOrder { get; set; } }
public class Education { public Guid Id { get; set; } = Guid.NewGuid(); public Guid CandidateProfileId { get; set; } public string Institution { get; set; } = string.Empty; public string Course { get; set; } = string.Empty; public string? Degree { get; set; } public DateOnly? CompletionDate { get; set; } public int DisplayOrder { get; set; } }
public class Certification { public Guid Id { get; set; } = Guid.NewGuid(); public Guid CandidateProfileId { get; set; } public string Name { get; set; } = string.Empty; public string? Issuer { get; set; } public DateOnly? IssuedAt { get; set; } public string? CredentialUrl { get; set; } public int DisplayOrder { get; set; } }
