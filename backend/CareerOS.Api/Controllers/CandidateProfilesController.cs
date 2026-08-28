using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CareerOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/candidate-profiles")]
public class CandidateProfilesController(
    CareerDbContext db,
    ICurrentUser currentUser,
    UserManager<ApplicationUser>? userManager = null) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateProfile>>> GetAll()
    {
        if (currentUser.CandidateProfileId is not { } profileId || profileId == Guid.Empty)
        {
            return Ok(Array.Empty<CandidateProfile>());
        }

        var profiles = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .Where(x => x.Id == profileId)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return Ok(profiles);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Get(Guid id)
    {
        if (currentUser.CandidateProfileId == null || currentUser.CandidateProfileId == Guid.Empty || currentUser.CandidateProfileId != id)
        {
            return NotFound();
        }

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        return profile is not null ? Ok(profile) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<CandidateProfile>> Create(CandidateProfileRequest request)
    {
        if (currentUser.CandidateProfileId is not null && currentUser.CandidateProfileId != Guid.Empty)
        {
            return Conflict(new { message = "User already has a candidate profile." });
        }

        var validationError = ValidateRequest(request);
        if (validationError != null) return BadRequest(new { message = validationError });

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

        return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Update(Guid id, CandidateProfileRequest request)
    {
        if (currentUser.CandidateProfileId == null || currentUser.CandidateProfileId == Guid.Empty || currentUser.CandidateProfileId != id)
        {
            return NotFound();
        }

        var validationError = ValidateRequest(request);
        if (validationError != null) return BadRequest(new { message = validationError });

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile is null) return NotFound();

        db.WorkExperiences.RemoveRange(profile.WorkExperiences);
        db.EducationHistory.RemoveRange(profile.EducationHistory);
        db.Certifications.RemoveRange(profile.Certifications);

        profile.WorkExperiences.Clear();
        profile.EducationHistory.Clear();
        profile.Certifications.Clear();

        Apply(profile, request);
        await db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (currentUser.CandidateProfileId == null || currentUser.CandidateProfileId == Guid.Empty || currentUser.CandidateProfileId != id)
        {
            return NotFound();
        }

        var profile = await db.CandidateProfiles
            .Include(x => x.WorkExperiences)
            .Include(x => x.EducationHistory)
            .Include(x => x.Certifications)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile is null) return NotFound();

        db.WorkExperiences.RemoveRange(profile.WorkExperiences);
        db.EducationHistory.RemoveRange(profile.EducationHistory);
        db.Certifications.RemoveRange(profile.Certifications);

        db.CandidateProfiles.Remove(profile);
        await db.SaveChangesAsync();
        return NoContent();
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
