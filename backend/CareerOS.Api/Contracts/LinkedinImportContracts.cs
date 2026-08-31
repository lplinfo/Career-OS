using System;
using System.Collections.Generic;

namespace CareerOS.Api.Contracts;

public class ParsedWorkExperienceDto
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class ParsedEducationDto
{
    public string Institution { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public DateOnly? CompletionDate { get; set; }
    public int DisplayOrder { get; set; }
}

public class ParsedCertificationDto
{
    public string Name { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public DateOnly? IssuedAt { get; set; }
    public string? CredentialUrl { get; set; }
    public int DisplayOrder { get; set; }
}

public class ParsedCandidateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string ProfessionalTitle { get; set; } = string.Empty;
    public string? ProfessionalSummary { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public List<ParsedWorkExperienceDto> WorkExperiences { get; set; } = [];
    public List<ParsedEducationDto> EducationHistory { get; set; } = [];
    public List<ParsedCertificationDto> Certifications { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public List<string> Languages { get; set; } = [];
}

public class GapItemDto
{
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActionableRecommendation { get; set; }
}

public class GapAnalysisDto
{
    public int CompletenessScore { get; set; }
    public List<string> MissingFields { get; set; } = [];
    public List<GapItemDto> Recommendations { get; set; } = [];
}

public class LinkedinImportResponseDto
{
    public ParsedCandidateProfileDto ParsedProfile { get; set; } = new();
    public GapAnalysisDto GapAnalysis { get; set; } = new();
}
