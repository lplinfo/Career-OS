using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Services;

public class CandidateProfileService(
    CareerDbContext db,
    ICurrentUser currentUser,
    ILinkedinParserService linkedinParser,
    ILinkedinGapAnalysisService gapAnalysisService,
    UserManager<ApplicationUser>? userManager = null) : ICandidateProfileService
{
    public async Task<IEnumerable<CandidateProfileResponse>> GetAllAsync()
    {
        if (currentUser.CandidateProfileId is not { } profileId || profileId == Guid.Empty)
        {
            return Array.Empty<CandidateProfileResponse>();
        }

        var profiles = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .Where(x => x.Id == profileId)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return profiles.Select(x => x.ToResponse());
    }

    public async Task<CandidateProfileResponse?> GetByIdAsync(Guid id)
    {
        if (currentUser.CandidateProfileId == null || currentUser.CandidateProfileId == Guid.Empty || currentUser.CandidateProfileId != id)
        {
            return null;
        }

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        return profile?.ToResponse();
    }

    public async Task<(CandidateProfileResponse? Profile, string? Error, bool IsConflict)> CreateAsync(CandidateProfileRequest request)
    {
        if (currentUser.CandidateProfileId is not null && currentUser.CandidateProfileId != Guid.Empty)
        {
            return (null, "User already has a candidate profile.", true);
        }

        var validationError = ValidateRequest(request);
        if (validationError != null)
        {
            return (null, validationError, false);
        }

        var profile = new CandidateProfile();
        db.CandidateProfiles.Add(profile);
        Apply(profile, request);
        await db.SaveChangesAsync();

        if (currentUser.UserId != Guid.Empty && userManager != null)
        {
            var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
            if (user != null)
            {
                user.CandidateProfileId = profile.Id;
                await userManager.UpdateAsync(user);
            }
        }

        return (profile.ToResponse(), null, false);
    }

    public async Task<(CandidateProfileResponse? Profile, string? Error, bool IsNotFound)> UpdateAsync(Guid id, CandidateProfileRequest request)
    {
        if (currentUser.CandidateProfileId == null || currentUser.CandidateProfileId == Guid.Empty || currentUser.CandidateProfileId != id)
        {
            return (null, null, true);
        }

        var validationError = ValidateRequest(request);
        if (validationError != null)
        {
            return (null, validationError, false);
        }

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile is null)
        {
            return (null, null, true);
        }

        db.WorkExperiences.RemoveRange(profile.WorkExperiences);
        db.EducationHistory.RemoveRange(profile.EducationHistory);
        db.Certifications.RemoveRange(profile.Certifications);

        profile.WorkExperiences.Clear();
        profile.EducationHistory.Clear();
        profile.Certifications.Clear();

        Apply(profile, request);
        await db.SaveChangesAsync();

        return (profile.ToResponse(), null, false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (currentUser.CandidateProfileId == null || currentUser.CandidateProfileId == Guid.Empty || currentUser.CandidateProfileId != id)
        {
            return false;
        }

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile is null)
        {
            return false;
        }

        db.WorkExperiences.RemoveRange(profile.WorkExperiences);
        db.EducationHistory.RemoveRange(profile.EducationHistory);
        db.Certifications.RemoveRange(profile.Certifications);

        db.CandidateProfiles.Remove(profile);
        await db.SaveChangesAsync();

        return true;
    }

    public (LinkedinImportResponseDto? Result, string? Error) ImportLinkedin(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (null, "Um arquivo PDF válido do LinkedIn é obrigatório.");
        }

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Apenas arquivos com formato PDF são aceitos.");
        }

        using var stream = file.OpenReadStream();
        var parsedProfile = linkedinParser.ParsePdf(stream);
        var gapAnalysis = gapAnalysisService.Analyze(parsedProfile);

        return (new LinkedinImportResponseDto
        {
            ParsedProfile = parsedProfile,
            GapAnalysis = gapAnalysis
        }, null);
    }

    private static void Apply(CandidateProfile profile, CandidateProfileRequest request)
    {
        profile.FullName = request.FullName;
        profile.PreferredName = request.PreferredName;
        profile.ProfessionalTitle = request.ProfessionalTitle;
        profile.ProfessionalSummary = request.ProfessionalSummary;
        profile.Email = request.Email;
        profile.Phone = request.Phone;
        profile.City = request.City;
        profile.Region = request.Region;
        profile.Country = request.Country;
        profile.OpenToRemoteWork = request.OpenToRemoteWork;
        profile.OpenToRelocation = request.OpenToRelocation;
        profile.WorkExperiences = (request.WorkExperiences ?? []).Select(x => new WorkExperience { Company = x.Company, Role = x.Role, StartDate = x.StartDate, EndDate = x.EndDate, IsCurrent = x.IsCurrent, Description = x.Description, DisplayOrder = x.DisplayOrder }).ToList();
        profile.EducationHistory = (request.EducationHistory ?? []).Select(x => new Education { Institution = x.Institution, Course = x.Course, Degree = x.Degree, CompletionDate = x.CompletionDate, DisplayOrder = x.DisplayOrder }).ToList();
        profile.Certifications = (request.Certifications ?? []).Select(x => new Certification { Name = x.Name, Issuer = x.Issuer, IssuedAt = x.IssuedAt, CredentialUrl = x.CredentialUrl, DisplayOrder = x.DisplayOrder }).ToList();
        profile.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? ValidateRequest(CandidateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return "FullName is required.";
        if (string.IsNullOrWhiteSpace(request.ProfessionalTitle)) return "ProfessionalTitle is required.";
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@') || !request.Email.Contains('.')) return "A valid email is required.";
        return null;
    }
}
