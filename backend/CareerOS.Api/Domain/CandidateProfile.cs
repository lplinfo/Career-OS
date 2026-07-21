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

    // Navigation properties / Collections
    public List<WorkExperience> Experiences { get; set; } = [];
    public List<Education> Educations { get; set; } = [];
    public List<Certification> Certifications { get; set; } = [];
    public List<Resume> Resumes { get; set; } = [];
}
