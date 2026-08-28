using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CareerOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/resumes")]
public class ResumesController(CareerDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Resume>>> GetAll()
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return Ok(Array.Empty<Resume>());
        }

        var resumes = await db.Resumes
            .Where(x => x.CandidateProfileId == profileId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return Ok(resumes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Resume>> Get(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return NotFound();
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        return resume is not null ? Ok(resume) : NotFound();
    }

    [HttpGet("by-candidate/{candidateProfileId:guid}")]
    public async Task<ActionResult<IEnumerable<Resume>>> GetByCandidate(Guid candidateProfileId)
    {
        if (currentUser.CandidateProfileId is not { } profileId || profileId != candidateProfileId)
        {
            return NotFound();
        }

        var resumes = await db.Resumes
            .Where(x => x.CandidateProfileId == profileId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return Ok(resumes);
    }

    [HttpPost]
    public async Task<ActionResult<Resume>> Create(ResumeRequest request)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return BadRequest(new { message = "User does not have an associated candidate profile." });
        }

        var resume = new Resume();
        Apply(resume, request, profileId);
        db.Resumes.Add(resume);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = resume.Id }, resume);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Resume>> Update(Guid id, ResumeRequest request)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return NotFound();
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null) return NotFound();

        Apply(resume, request, profileId);
        await db.SaveChangesAsync();
        return Ok(resume);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return NotFound();
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null) return NotFound();

        db.Resumes.Remove(resume);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return NotFound();
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null) return NotFound();

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == profileId);

        if (profile is null) return NotFound();

        var pdfBytes = ExportService.GeneratePdf(resume, profile);
        return File(pdfBytes, "application/pdf", $"resume_{resume.Language}.pdf");
    }

    [HttpGet("{id:guid}/export/docx")]
    public async Task<IActionResult> ExportDocx(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return NotFound();
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null) return NotFound();

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == profileId);

        if (profile is null) return NotFound();

        var docxBytes = ExportService.GenerateDocx(resume, profile);
        return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"resume_{resume.Language}.docx");
    }

    [HttpGet("{id:guid}/export/ats")]
    public async Task<IActionResult> ExportAts(Guid id)
    {
        if (currentUser.CandidateProfileId is not { } profileId)
        {
            return NotFound();
        }

        var resume = await db.Resumes.FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == profileId);
        if (resume is null) return NotFound();

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == profileId);

        if (profile is null) return NotFound();

        var text = ExportService.GenerateAtsText(resume, profile);
        return Content(text, "text/plain", System.Text.Encoding.UTF8);
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
