using System;

namespace CareerOS.Api.Contracts;

public class ResumeResponse
{
    public Guid Id { get; set; }
    public Guid CandidateProfileId { get; set; }
    public string Language { get; set; } = "pt";
    public string TargetCountry { get; set; } = "BR";
    public bool ShowPhone { get; set; } = true;
    public bool ShowEmail { get; set; } = true;
    public bool ShowLocation { get; set; } = true;
    public string CustomizedTitle { get; set; } = string.Empty;
    public string CustomizedSummary { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string? CustomizedExperiencesJson { get; set; }
    public string? CustomizedEducationsJson { get; set; }
    public string? CustomizedCertificationsJson { get; set; }
}
