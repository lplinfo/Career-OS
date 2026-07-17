using System;
using System.ComponentModel.DataAnnotations;

namespace CareerOS.Api.Contracts;

public class ResumeRequest
{
    public Guid CandidateProfileId { get; set; }
    [Required, StringLength(10)]
    public string Language { get; set; } = "pt";
    [Required, StringLength(10)]
    public string TargetCountry { get; set; } = "BR";
    public bool ShowPhone { get; set; } = true;
    public bool ShowEmail { get; set; } = true;
    public bool ShowLocation { get; set; } = true;
    [Required, StringLength(200)]
    public string CustomizedTitle { get; set; } = string.Empty;
    [Required, StringLength(4000)]
    public string CustomizedSummary { get; set; } = string.Empty;
    [Required]
    public string Skills { get; set; } = string.Empty;
    public string? CustomizedExperiencesJson { get; set; }
    public string? CustomizedEducationsJson { get; set; }
    public string? CustomizedCertificationsJson { get; set; }
}
