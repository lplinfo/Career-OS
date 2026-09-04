using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Services;

public class ResumeService(CareerDbContext db, ICurrentUser currentUser) : IResumeService
{
    public async Task<IEnumerable<ResumeResponse>> GetAllAsync()
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return Array.Empty<ResumeResponse>();
        }

        var resumes = await db.Resumes
            .Where(x => x.CandidateProfileId == profileId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return resumes.Select(x => x.ToResponse());
    }

    public async Task<ResumeResponse?> GetByIdAsync(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return null;
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        return resume?.ToResponse();
    }

    public async Task<IEnumerable<ResumeResponse>?> GetByCandidateAsync(Guid candidateProfileId)
    {
        if (currentUser.CandidateProfileId is not { } profileId || profileId != candidateProfileId)
        {
            return null;
        }

        var resumes = await db.Resumes
            .Where(x => x.CandidateProfileId == profileId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return resumes.Select(x => x.ToResponse());
    }

    public async Task<(ResumeResponse? Resume, string? Error)> CreateAsync(ResumeRequest request)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return (null, "User does not have an associated candidate profile.");
        }

        var resume = new Resume();
        Apply(resume, request, profileId);
        db.Resumes.Add(resume);
        await db.SaveChangesAsync();

        return (resume.ToResponse(), null);
    }

    public async Task<(ResumeResponse? Resume, string? Error, bool IsNotFound)> UpdateAsync(Guid id, ResumeRequest request)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return (null, null, true);
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null)
        {
            return (null, null, true);
        }

        Apply(resume, request, profileId);
        await db.SaveChangesAsync();

        return (resume.ToResponse(), null, false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return false;
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null)
        {
            return false;
        }

        db.Resumes.Remove(resume);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<(byte[] FileBytes, string FileName, string ContentType)?> ExportPdfAsync(Guid id)
    {
        var resumeAndProfile = await GetResumeAndProfileAsync(id);
        if (resumeAndProfile is null) return null;

        var (resume, profile) = resumeAndProfile.Value;
        var pdfBytes = ExportService.GeneratePdf(resume, profile);
        return (pdfBytes, $"resume_{resume.Language}.pdf", "application/pdf");
    }

    public async Task<(byte[] FileBytes, string FileName, string ContentType)?> ExportDocxAsync(Guid id)
    {
        var resumeAndProfile = await GetResumeAndProfileAsync(id);
        if (resumeAndProfile is null) return null;

        var (resume, profile) = resumeAndProfile.Value;
        var docxBytes = ExportService.GenerateDocx(resume, profile);
        return (docxBytes, $"resume_{resume.Language}.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    public async Task<(string Content, string ContentType)?> ExportAtsAsync(Guid id)
    {
        var resumeAndProfile = await GetResumeAndProfileAsync(id);
        if (resumeAndProfile is null) return null;

        var (resume, profile) = resumeAndProfile.Value;
        var text = ExportService.GenerateAtsText(resume, profile);
        return (text, "text/plain");
    }

    private async Task<(Resume Resume, CandidateProfile Profile)?> GetResumeAndProfileAsync(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return null;
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null) return null;

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == profileId);

        if (profile is null) return null;

        return (resume, profile);
    }

    private static void Apply(Resume resume, ResumeRequest request, Guid candidateProfileId)
    {
        resume.CandidateProfileId = candidateProfileId;
        resume.Language = request.Language;
        resume.TargetCountry = request.TargetCountry;
        resume.ShowPhone = request.ShowPhone;
        resume.ShowEmail = request.ShowEmail;
        resume.ShowLocation = request.ShowLocation;
        resume.CustomizedTitle = request.CustomizedTitle;
        resume.CustomizedSummary = request.CustomizedSummary;
        resume.Skills = request.Skills;
        resume.CustomizedExperiencesJson = request.CustomizedExperiencesJson;
        resume.CustomizedEducationsJson = request.CustomizedEducationsJson;
        resume.CustomizedCertificationsJson = request.CustomizedCertificationsJson;
        resume.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
