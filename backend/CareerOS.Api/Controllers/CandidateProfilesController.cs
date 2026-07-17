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
    public async Task<ActionResult<IEnumerable<CandidateProfile>>> GetAll() =>
        Ok(await db.CandidateProfiles
            .Include(x => x.Experiences)
            .Include(x => x.Educations)
            .Include(x => x.Certifications)
            .OrderBy(x => x.FullName)
            .ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Get(Guid id) =>
        await db.CandidateProfiles
            .Include(x => x.Experiences)
            .Include(x => x.Educations)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id) is { } profile ? Ok(profile) : NotFound();

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
        var profile = await db.CandidateProfiles.FindAsync(id);
        if (profile is null) return NotFound();
        db.CandidateProfiles.Remove(profile);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidateRequest(CandidateProfileRequest request)
    {
        if (request.Experiences != null)
        {
            foreach (var exp in request.Experiences)
            {
                if (!exp.IsCurrent && exp.EndDate.HasValue && exp.StartDate > exp.EndDate.Value)
                {
                    return $"Inconsistência na experiência '{exp.CompanyName}': data de início deve ser anterior à de término.";
                }
            }
        }

        if (request.Educations != null)
        {
            foreach (var edu in request.Educations)
            {
                if (!edu.IsCurrent && edu.EndDate.HasValue && edu.StartDate > edu.EndDate.Value)
                {
                    return $"Inconsistência na formação '{edu.Institution}': data de início deve ser anterior à de término.";
                }
            }
        }

        return null;
    }

    private void Apply(CandidateProfile profile, CandidateProfileRequest request)
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
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        // Apply Experiences
        if (request.Experiences != null)
        {
            foreach (var r in request.Experiences)
            {
                var exp = new WorkExperience
                {
                    Id = r.Id ?? Guid.NewGuid(),
                    CandidateProfileId = profile.Id,
                    CompanyName = r.CompanyName,
                    JobTitle = r.JobTitle,
                    Description = r.Description,
                    StartDate = DateTime.SpecifyKind(r.StartDate, DateTimeKind.Utc),
                    EndDate = r.EndDate.HasValue ? DateTime.SpecifyKind(r.EndDate.Value, DateTimeKind.Utc) : null,
                    IsCurrent = r.IsCurrent,
                    Order = r.Order
                };
                profile.Experiences.Add(exp);
                db.WorkExperiences.Add(exp);
            }
        }

        // Apply Educations
        if (request.Educations != null)
        {
            foreach (var r in request.Educations)
            {
                var edu = new Education
                {
                    Id = r.Id ?? Guid.NewGuid(),
                    CandidateProfileId = profile.Id,
                    Institution = r.Institution,
                    Degree = r.Degree,
                    FieldOfStudy = r.FieldOfStudy,
                    StartDate = DateTime.SpecifyKind(r.StartDate, DateTimeKind.Utc),
                    EndDate = r.EndDate.HasValue ? DateTime.SpecifyKind(r.EndDate.Value, DateTimeKind.Utc) : null,
                    IsCurrent = r.IsCurrent,
                    Order = r.Order
                };
                profile.Educations.Add(edu);
                db.Educations.Add(edu);
            }
        }

        // Apply Certifications
        if (request.Certifications != null)
        {
            foreach (var r in request.Certifications)
            {
                var cert = new Certification
                {
                    Id = r.Id ?? Guid.NewGuid(),
                    CandidateProfileId = profile.Id,
                    Name = r.Name,
                    IssuingOrganization = r.IssuingOrganization,
                    IssueDate = r.IssueDate.HasValue ? DateTime.SpecifyKind(r.IssueDate.Value, DateTimeKind.Utc) : null,
                    ExpirationDate = r.ExpirationDate.HasValue ? DateTime.SpecifyKind(r.ExpirationDate.Value, DateTimeKind.Utc) : null,
                    CredentialId = r.CredentialId,
                    CredentialUrl = r.CredentialUrl,
                    Order = r.Order
                };
                profile.Certifications.Add(cert);
                db.Certifications.Add(cert);
            }
        }
    }
}
