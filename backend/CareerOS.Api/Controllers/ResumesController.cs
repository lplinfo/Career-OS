using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CareerOS.Api.Controllers;

[ApiController]
[Route("api/resumes")]
public class ResumesController(CareerDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Resume>>> GetAll() =>
        Ok(await db.Resumes.OrderByDescending(x => x.UpdatedAt).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Resume>> Get(Guid id) =>
        await db.Resumes.FindAsync(id) is { } resume ? Ok(resume) : NotFound();

    [HttpGet("by-candidate/{candidateProfileId:guid}")]
    public async Task<ActionResult<IEnumerable<Resume>>> GetByCandidate(Guid candidateProfileId) =>
        Ok(await db.Resumes.Where(x => x.CandidateProfileId == candidateProfileId).OrderByDescending(x => x.UpdatedAt).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<Resume>> Create(ResumeRequest request)
    {
        var resume = new Resume();
        Apply(resume, request);
        db.Resumes.Add(resume);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = resume.Id }, resume);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Resume>> Update(Guid id, ResumeRequest request)
    {
        var resume = await db.Resumes.FindAsync(id);
        if (resume is null) return NotFound();
        Apply(resume, request);
        await db.SaveChangesAsync();
        return Ok(resume);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resume = await db.Resumes.FindAsync(id);
        if (resume is null) return NotFound();
        db.Resumes.Remove(resume);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var resume = await db.Resumes.FindAsync(id);
        if (resume is null) return NotFound();

        var profile = await db.CandidateProfiles
            .Include(x => x.Experiences)
            .Include(x => x.Educations)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == resume.CandidateProfileId);

        if (profile is null) return NotFound();

        var pdfBytes = ExportService.GeneratePdf(resume, profile);
        return File(pdfBytes, "application/pdf", $"resume_{resume.Language}.pdf");
    }

    [HttpGet("{id:guid}/export/docx")]
    public async Task<IActionResult> ExportDocx(Guid id)
    {
        var resume = await db.Resumes.FindAsync(id);
        if (resume is null) return NotFound();

        var profile = await db.CandidateProfiles
            .Include(x => x.Experiences)
            .Include(x => x.Educations)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == resume.CandidateProfileId);

        if (profile is null) return NotFound();

        var docxBytes = ExportService.GenerateDocx(resume, profile);
        return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"resume_{resume.Language}.docx");
    }

    [HttpGet("{id:guid}/export/ats")]
    public async Task<IActionResult> ExportAts(Guid id)
    {
        var resume = await db.Resumes.FindAsync(id);
        if (resume is null) return NotFound();

        var profile = await db.CandidateProfiles
            .Include(x => x.Experiences)
            .Include(x => x.Educations)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == resume.CandidateProfileId);

        if (profile is null) return NotFound();

        var text = ExportService.GenerateAtsText(resume, profile);
        return Content(text, "text/plain", System.Text.Encoding.UTF8);
    }

    private static void Apply(Resume resume, ResumeRequest request)
    {
        resume.CandidateProfileId = request.CandidateProfileId;
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
