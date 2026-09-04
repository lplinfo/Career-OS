using System.Linq;
using CareerOS.Api.Domain;

namespace CareerOS.Api.Contracts;

public static class MappingExtensions
{
    public static CandidateProfileResponse ToResponse(this CandidateProfile profile)
    {
        return new CandidateProfileResponse
        {
            Id = profile.Id,
            FullName = profile.FullName,
            PreferredName = profile.PreferredName,
            ProfessionalTitle = profile.ProfessionalTitle,
            ProfessionalSummary = profile.ProfessionalSummary,
            Email = profile.Email,
            Phone = profile.Phone,
            City = profile.City,
            Region = profile.Region,
            Country = profile.Country,
            OpenToRemoteWork = profile.OpenToRemoteWork,
            OpenToRelocation = profile.OpenToRelocation,
            WorkExperiences = profile.WorkExperiences.Select(x => new WorkExperienceResponse
            {
                Company = x.Company,
                Role = x.Role,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsCurrent = x.IsCurrent,
                Description = x.Description,
                DisplayOrder = x.DisplayOrder
            }).ToList(),
            EducationHistory = profile.EducationHistory.Select(x => new EducationResponse
            {
                Institution = x.Institution,
                Course = x.Course,
                Degree = x.Degree,
                CompletionDate = x.CompletionDate,
                DisplayOrder = x.DisplayOrder
            }).ToList(),
            Certifications = profile.Certifications.Select(x => new CertificationResponse
            {
                Name = x.Name,
                Issuer = x.Issuer,
                IssuedAt = x.IssuedAt,
                CredentialUrl = x.CredentialUrl,
                DisplayOrder = x.DisplayOrder
            }).ToList()
        };
    }

    public static ResumeResponse ToResponse(this Resume resume)
    {
        return new ResumeResponse
        {
            Id = resume.Id,
            CandidateProfileId = resume.CandidateProfileId,
            Language = resume.Language,
            TargetCountry = resume.TargetCountry,
            ShowPhone = resume.ShowPhone,
            ShowEmail = resume.ShowEmail,
            ShowLocation = resume.ShowLocation,
            CustomizedTitle = resume.CustomizedTitle,
            CustomizedSummary = resume.CustomizedSummary,
            Skills = resume.Skills,
            CustomizedExperiencesJson = resume.CustomizedExperiencesJson,
            CustomizedEducationsJson = resume.CustomizedEducationsJson,
            CustomizedCertificationsJson = resume.CustomizedCertificationsJson
        };
    }
}
