using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CareerOS.Api.Controllers;

[ApiController]
[Route("api/candidate-profiles")]
public class CandidateProfilesController(CareerDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateProfile>>> GetAll() => Ok(await db.CandidateProfiles.Include(x => x.WorkExperiences).Include(x => x.EducationHistory).Include(x => x.Certifications).OrderBy(x => x.FullName).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Get(Guid id) => await db.CandidateProfiles.Include(x => x.WorkExperiences).Include(x => x.EducationHistory).Include(x => x.Certifications).FirstOrDefaultAsync(x => x.Id == id) is { } profile ? Ok(profile) : NotFound();

    [HttpPost]
    public async Task<ActionResult<CandidateProfile>> Create(CandidateProfileRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null) return BadRequest(new { message = validationError });

        var profile = new CandidateProfile();
        db.CandidateProfiles.Add(profile);
        Apply(profile, request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Update(Guid id, CandidateProfileRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null) return BadRequest(new { message = validationError });

        var profile = await db.CandidateProfiles
            .Include(x => x.Experiences)
            .Include(x => x.Educations)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile is null) return NotFound();

        // Remove existing items explicitly
        db.WorkExperiences.RemoveRange(profile.Experiences);
        db.Educations.RemoveRange(profile.Educations);
        db.Certifications.RemoveRange(profile.Certifications);

        profile.Experiences.Clear();
        profile.Educations.Clear();
        profile.Certifications.Clear();

        Apply(profile, request);
        await db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        profile.FullName = request.FullName; profile.PreferredName = request.PreferredName; profile.ProfessionalTitle = request.ProfessionalTitle;
        profile.ProfessionalSummary = request.ProfessionalSummary; profile.Email = request.Email; profile.Phone = request.Phone;
        profile.City = request.City; profile.Region = request.Region; profile.Country = request.Country;
        profile.OpenToRemoteWork = request.OpenToRemoteWork; profile.OpenToRelocation = request.OpenToRelocation; profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.WorkExperiences = (request.WorkExperiences ?? []).Select(x => new WorkExperience { Company = x.Company, Role = x.Role, StartDate = x.StartDate, EndDate = x.EndDate, IsCurrent = x.IsCurrent, Description = x.Description, DisplayOrder = x.DisplayOrder }).ToList();
        profile.EducationHistory = (request.EducationHistory ?? []).Select(x => new Education { Institution = x.Institution, Course = x.Course, Degree = x.Degree, CompletionDate = x.CompletionDate, DisplayOrder = x.DisplayOrder }).ToList();
        profile.Certifications = (request.Certifications ?? []).Select(x => new Certification { Name = x.Name, Issuer = x.Issuer, IssuedAt = x.IssuedAt, CredentialUrl = x.CredentialUrl, DisplayOrder = x.DisplayOrder }).ToList();
    }
}
