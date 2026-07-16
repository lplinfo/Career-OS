using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Controllers;

[ApiController]
[Route("api/candidate-profiles")]
public class CandidateProfilesController(CareerDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateProfile>>> GetAll() => Ok(await db.CandidateProfiles.OrderBy(x => x.FullName).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Get(Guid id) => await db.CandidateProfiles.FindAsync(id) is { } profile ? Ok(profile) : NotFound();

    [HttpPost]
    public async Task<ActionResult<CandidateProfile>> Create(CandidateProfileRequest request)
    {
        var profile = new CandidateProfile();
        Apply(profile, request);
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { profile.Id }, profile);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CandidateProfile>> Update(Guid id, CandidateProfileRequest request)
    {
        var profile = await db.CandidateProfiles.FindAsync(id);
        if (profile is null) return NotFound();
        Apply(profile, request);
        await db.SaveChangesAsync();
        return Ok(profile);
    }

    private static void Apply(CandidateProfile profile, CandidateProfileRequest request)
    {
        profile.FullName = request.FullName; profile.PreferredName = request.PreferredName; profile.ProfessionalTitle = request.ProfessionalTitle;
        profile.ProfessionalSummary = request.ProfessionalSummary; profile.Email = request.Email; profile.Phone = request.Phone;
        profile.City = request.City; profile.Region = request.Region; profile.Country = request.Country;
        profile.OpenToRemoteWork = request.OpenToRemoteWork; profile.OpenToRelocation = request.OpenToRelocation; profile.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
